using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!TryGetDamagedEnemy(target, result, out uint combatId))
        {
            return;
        }

        TrackEnemyDamageReceived(target, combatId);
        await ApplyEnemyDamageReceivedReactiveHexes(target);

        if (!ShouldSuppressDuplicateEnemyThresholdTrigger(target, result, dealer, cardSource))
        {
            await ApplyEnemyThresholdHexes(target, combatId);
        }
    }

    private static bool TryGetDamagedEnemy(Creature target, DamageResult result, out uint combatId)
    {
        combatId = 0;
        if (target.Side != CombatSide.Enemy || result.UnblockedDamage <= 0 || target.CombatId == null)
        {
            return false;
        }

        combatId = target.CombatId.Value;
        return true;
    }

    private void TrackEnemyDamageReceived(Creature target, uint combatId)
    {
        if (HasActiveMonsterHex(MonsterHexKind.MountainSoul))
        {
            _combatTracking.MountainSoulDamagedSinceLastTurn.Add(combatId);
        }
    }

    private async Task ApplyEnemyDamageReceivedReactiveHexes(Creature target)
    {
        if (HasActiveMonsterHex(MonsterHexKind.BloodPact)
            && target.IsAlive
            && TryConsumeLimitedProc(_combatTracking.BloodPactProcsThisTurn, target, 2))
        {
            await PowerCmd.Apply<HextechBloodPactTemporaryStrengthPower>(target, BloodPactTemporaryStrengthStacks, target, null);
        }

        if (HasActiveMonsterHex(MonsterHexKind.ClownCollege)
            && target.IsAlive
            && TryConsumeLimitedProc(_combatTracking.ClownCollegeProcsThisTurn, target, 1))
        {
            await HextechEnemyPowerScalingHooks.Apply<SlipperyPower>(target, ClownCollegeSlipperyStacks, target, null);
        }
    }

    private async Task ApplyEnemyThresholdHexes(Creature target, uint combatId)
    {
        if (!IsBelowEnemyHealthThreshold(target))
        {
            return;
        }

        if (HasActiveMonsterHex(MonsterHexKind.EscapePlan))
        {
            TryQueueEnemyThresholdEffect(_combatTracking.EscapePlanTriggered, _combatTracking.EscapePlanPending, combatId);
        }

        if (HasActiveMonsterHex(MonsterHexKind.Repulsor))
        {
            TryQueueEnemyThresholdEffect(_combatTracking.RepulsorTriggered, _combatTracking.RepulsorPending, combatId);
        }

        if (HasActiveMonsterHex(MonsterHexKind.DawnbringersResolve)
            && _combatTracking.DawnTriggered.Add(combatId))
        {
            int regen = Math.Max(1, (int)Math.Floor(target.MaxHp * DawnbringersResolveRegenPercent));
            await HextechEnemyPowerScalingHooks.Apply<RegenPower>(target, regen, target, null);
        }

        if (HasActiveMonsterHex(MonsterHexKind.FeelTheBurn)
            && _combatTracking.FeelTheBurnTriggered.Add(combatId))
        {
            _combatTracking.FeelTheBurnPending.Add(combatId);
        }

        if (HasActiveMonsterHex(MonsterHexKind.MikaelsBlessing)
            && _combatTracking.MikaelsBlessingTriggers.GetValueOrDefault(combatId, 0) < MikaelsBlessingMaxTriggers)
        {
            _combatTracking.MikaelsBlessingTriggers[combatId] = _combatTracking.MikaelsBlessingTriggers.GetValueOrDefault(combatId, 0) + 1;
            int heal = Math.Max(1, (int)Math.Floor(target.MaxHp * MikaelsBlessingHealPercent));
            await CreatureCmd.Heal(target, heal);

            List<PowerModel> negativePowers = target.Powers
                .Where(static power => power.GetTypeForAmount(power.Amount) == PowerType.Debuff)
                .ToList();
            foreach (PowerModel power in negativePowers)
            {
                await PowerCmd.Remove(power);
            }
        }
    }

    private static bool IsBelowEnemyHealthThreshold(Creature target)
    {
        return target.CurrentHp < target.MaxHp * EscapePlanHealthThresholdPercent;
    }

    private static bool TryQueueEnemyThresholdEffect(HashSet<uint> triggered, HashSet<uint> pending, uint combatId)
    {
        if (!triggered.Add(combatId))
        {
            return false;
        }

        pending.Add(combatId);
        return true;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer?.Side != CombatSide.Enemy || dealer.CombatState?.RunState != RunState || !target.IsAlive)
        {
            return;
        }

        await ApplyEnemyDamageGivenImmediateHexes(dealer, result, target, cardSource);
        if (result.UnblockedDamage <= 0 || target.Side != CombatSide.Player)
        {
            return;
        }

        await ApplyEnemyDamageGivenPlayerHitHexes(dealer, target);
    }

    private async Task ApplyEnemyDamageGivenImmediateHexes(Creature dealer, DamageResult result, Creature target, CardModel? cardSource)
    {
        if (HasActiveMonsterHex(MonsterHexKind.ShrinkRay) && result.UnblockedDamage > 0 && target.Side == CombatSide.Player)
        {
            await PowerCmd.Apply<ShrinkPower>(target, ShrinkRayStacks, dealer, cardSource);
        }

        if (HasActiveMonsterHex(MonsterHexKind.Firebrand)
            && result.UnblockedDamage > 0
            && target.Side == CombatSide.Player
            && !HextechBurnPower.IsResolvingDamage)
        {
            await PowerCmd.Apply<HextechBurnPower>(target, FirebrandBurnStacks, dealer, cardSource);
        }

        if (HasActiveMonsterHex(MonsterHexKind.Goldrend)
            && result.UnblockedDamage > 0
            && target.Player != null)
        {
            await HextechGoldrendSync.HandleEnemyGoldrendHit(target.Player);
        }
    }

    private async Task ApplyEnemyDamageGivenPlayerHitHexes(Creature dealer, Creature target)
    {
        if (HasActiveMonsterHex(MonsterHexKind.DevilsDance)
            && dealer.IsAlive
            && dealer.CombatId != null
            && _combatTracking.DevilsDanceTriggeredThisTurn.Add(dealer.CombatId.Value))
        {
            int heal = Math.Max(1, (int)Math.Floor(dealer.MaxHp * DevilsDanceHealPercent));
            await CreatureCmd.Heal(dealer, heal);
        }

        if (HasActiveMonsterHex(MonsterHexKind.SpeedDemon)
            && dealer.IsAlive
            && dealer.CombatId != null)
        {
            _combatTracking.SpeedDemonPending.Add(dealer.CombatId.Value);
        }

        if (HasActiveMonsterHex(MonsterHexKind.CantTouchThis) && dealer.IsAlive)
        {
            await HextechEnemyPowerScalingHooks.Apply<SlipperyPower>(dealer, CantTouchThisSlipperyStacks, dealer, null);
        }

        if (HasActiveMonsterHex(MonsterHexKind.FeyMagic)
            && target.CombatId != null
            && dealer.CombatId != null
            && !_combatTracking.FeyMagicPendingNoDrawPlayers.ContainsKey(target.CombatId.Value))
        {
            _combatTracking.FeyMagicPendingNoDrawPlayers[target.CombatId.Value] = dealer.CombatId.Value;
        }

        if (HasActiveMonsterHex(MonsterHexKind.FinalForm) && dealer.IsAlive)
        {
            int block = Math.Max(1, (int)Math.Floor(dealer.MaxHp * FinalFormBlockPercent));
            await CreatureCmd.GainBlock(dealer, block, ValueProp.Unpowered, null);
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        TrackPlayerAttackCardPlayed(cardPlay);

        if (!HasActiveMonsterHex(MonsterHexKind.MasterOfDuality)
            || cardPlay.Card.Owner?.Creature.Side != CombatSide.Player)
        {
            return;
        }

        Creature playerCreature = cardPlay.Card.Owner.Creature;
        if (!playerCreature.IsAlive)
        {
            return;
        }

        if (cardPlay.Card.Type == MegaCrit.Sts2.Core.Entities.Cards.CardType.Skill)
        {
            await PowerCmd.Apply<HextechTemporaryStrengthLossPower>(playerCreature, 1m, playerCreature, cardPlay.Card);
        }
        else if (cardPlay.Card.Type == MegaCrit.Sts2.Core.Entities.Cards.CardType.Attack)
        {
            await PowerCmd.Apply<HextechTemporaryDexterityLossPower>(playerCreature, 1m, playerCreature, cardPlay.Card);
        }
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (!HasActiveMonsterHex(MonsterHexKind.WarmogsSpirit)
            || card.Owner?.Creature.Side != CombatSide.Player
            || card.Owner.Creature.CombatState?.RunState != RunState)
        {
            return;
        }

        if (IsNetworkMultiplayer())
        {
            return;
        }

        Player owner = card.Owner;
        ulong playerId = owner.NetId;
        int cardsDrawn = _combatTracking.PlayerCardsDrawnThisCombat.GetValueOrDefault(playerId, 0) + 1;
        _combatTracking.PlayerCardsDrawnThisCombat[playerId] = cardsDrawn;
        if (cardsDrawn % 8 != 0)
        {
            return;
        }

        HextechCombatState combatState = owner.Creature.CombatState;
        foreach (Creature enemy in GetAliveEnemies(combatState))
        {
            await HextechEnemyPowerScalingHooks.Apply<PlatingPower>(enemy, 1m, enemy, null);
        }
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsNetworkMultiplayer() && cardPlay.Card.Owner?.Creature.CombatState is HextechCombatState combatStateForWarmogs)
        {
            await ResolveWarmogsSpiritDrawProgressFromHistory(combatStateForWarmogs);
        }

        Player? owner = cardPlay.Card.Owner;
        if (owner == null
            || cardPlay.Card.Type != CardType.Power
            || owner.Creature.CombatState?.RunState != RunState
            || owner.Creature.GetPower<StormPower>() is not StormPower stormPower)
        {
            return;
        }

        int lightningCount = Math.Max(0, (int)Math.Floor((decimal)stormPower.Amount));
        for (int i = 0; i < lightningCount; i++)
        {
            OrbModel orb = ModelDb.Orb<LightningOrb>().ToMutable();
            await OrbCmd.Channel(new BlockingPlayerChoiceContext(), orb, owner);
        }
    }

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (IsNetworkMultiplayer() && player.Creature.CombatState is HextechCombatState combatState)
        {
            await ResolveWarmogsSpiritDrawProgressFromHistory(combatState);
        }
    }

#if !STS2_104_OR_NEWER
    public override async Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (IsNetworkMultiplayer() && player.Creature.CombatState is HextechCombatState combatState)
        {
            await ResolveWarmogsSpiritDrawProgressFromHistory(combatState);
        }
    }
#endif

    private async Task ResolveWarmogsSpiritDrawProgressFromHistory(HextechCombatState combatState)
    {
        if (!HasActiveMonsterHex(MonsterHexKind.WarmogsSpirit)
            || combatState.RunState != RunState)
        {
            return;
        }

        int pendingPlating = 0;
        foreach (Player player in combatState.Players.OrderBy(static player => player.NetId))
        {
            int drawnCards = CountPlayerDrawnCardsFromHistory(player);
            int previousDrawnCards = _combatTracking.PlayerCardsDrawnThisCombat.GetValueOrDefault(player.NetId, 0);
            if (drawnCards <= previousDrawnCards)
            {
                continue;
            }

            pendingPlating += drawnCards / 8 - previousDrawnCards / 8;
            _combatTracking.PlayerCardsDrawnThisCombat[player.NetId] = drawnCards;
        }

        if (pendingPlating <= 0)
        {
            return;
        }

        foreach (Creature enemy in GetAliveEnemies(combatState))
        {
            await HextechEnemyPowerScalingHooks.Apply<PlatingPower>(enemy, pendingPlating, enemy, null);
        }
    }

    private static int CountPlayerDrawnCardsFromHistory(Player player)
    {
        return CombatManager.Instance.History.Entries
            .OfType<CardDrawnEntry>()
            .Count(entry => entry.Card.Owner?.NetId == player.NetId);
    }

    private static bool IsNetworkMultiplayer()
    {
        return RunManager.Instance.NetService.Type is NetGameType.Host or NetGameType.Client;
    }

#if STS2_104_OR_NEWER
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#else
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#endif
    {
        if (power is MinionPower && amount > 0m)
        {
            await TryApplyServantMasterIllusion(power.Owner, applier, cardSource);
        }

        bool hasMonsterDebuffTrigger = TryGetMonsterDebuffTrigger(power, amount, applier, out Creature? target, out Creature? source);
        bool suppressMonsterDebuffDuplicate = hasMonsterDebuffTrigger && ShouldSuppressMonsterDebuffDuplicate(power, amount, source, cardSource);
        if (hasMonsterDebuffTrigger && !suppressMonsterDebuffDuplicate)
        {
            if (HasActiveMonsterHex(MonsterHexKind.Slap)
                && TryConsumeLimitedProc(_combatTracking.SlapProcsThisTurn, source!, 3))
            {
                await PowerCmd.Apply<StrengthPower>(source!, 1m, source, null);
            }

            if (HasActiveMonsterHex(MonsterHexKind.Tormentor)
                && !_combatTracking.HandlingMonsterTormentorBurn
                && TryConsumeLimitedProc(_combatTracking.TormentorProcsThisTurn, source!, 5))
            {
                try
                {
                    _combatTracking.HandlingMonsterTormentorBurn = true;
                    await PowerCmd.Apply<HextechBurnPower>(target!, 2m, source, null);
                }
                finally
                {
                    _combatTracking.HandlingMonsterTormentorBurn = false;
                }
            }
        }

        Creature? courageSource = null;
        bool hasCourageTrigger = false;
        if (hasMonsterDebuffTrigger && !suppressMonsterDebuffDuplicate)
        {
            courageSource = source;
            hasCourageTrigger = courageSource != null;
        }
        else if (TryGetMonsterSelfBuffTrigger(power, amount, applier, out Creature? buffSource))
        {
            courageSource = buffSource;
            hasCourageTrigger = true;
        }

        if (HasActiveMonsterHex(MonsterHexKind.CourageOfColossus)
            && hasCourageTrigger
            && TryConsumeLimitedProc(_combatTracking.CourageProcsThisTurn, courageSource!, 1))
        {
            int plating = Math.Max(1, (int)Math.Floor(courageSource!.MaxHp * CourageOfColossusPlatingPercent));
            await HextechEnemyPowerScalingHooks.Apply<PlatingPower>(courageSource, plating, courageSource, null);
        }

    }

    public override async Task BeforeDeath(Creature creature)
    {
        if (!HasActiveMonsterHex(MonsterHexKind.GetExcited)
            || creature.Side != CombatSide.Enemy
            || creature.CombatState?.RunState != RunState)
        {
            return;
        }

        PainfulStabsPower? painfulStabs = creature.GetPower<PainfulStabsPower>();
        if (painfulStabs != null)
        {
            await PowerCmd.Remove(painfulStabs);
        }
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented
            || target.Side != CombatSide.Enemy
            || !HextechMonsterInteractionPolicy.IsTrueCombatDeath(target, out HextechCombatState? combatState))
        {
            return;
        }

        if (HasActiveMonsterHex(MonsterHexKind.Nightstalking))
        {
            IReadOnlyList<Creature> enemies = GetAliveEnemies(combatState)
                .Where(enemy => enemy != target)
                .ToList();
            if (enemies.Count > 0)
            {
                await PowerCmd.Apply<StrengthPower>(enemies, 1m, null, null);
                await PowerCmd.Apply<PaperCutsPower>(enemies, 1m, null, null);
            }
        }

        if (HasActiveMonsterHex(MonsterHexKind.GetExcited))
        {
            IReadOnlyList<Creature> enemies = GetAliveEnemies(combatState)
                .Where(enemy => enemy != target)
                .ToList();
            if (enemies.Count > 0)
            {
                await PowerCmd.Apply<StrengthPower>(enemies, 1m, null, null);
                await PowerCmd.Apply<PainfulStabsPower>(enemies, 1m, null, null);
            }
        }
    }
}
