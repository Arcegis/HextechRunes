using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
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
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target.Side != CombatSide.Enemy || result.UnblockedDamage <= 0 || target.CombatId == null)
        {
            return;
        }

        uint combatId = target.CombatId.Value;
        if (HasActiveMonsterHex(MonsterHexKind.MountainSoul))
        {
            _mountainSoulDamagedSinceLastTurn.Add(combatId);
        }

        if (HasActiveMonsterHex(MonsterHexKind.BloodPact)
            && target.IsAlive
            && TryConsumeLimitedProc(_bloodPactProcsThisTurn, target, 2))
        {
            await PowerCmd.Apply<HextechBloodPactTemporaryStrengthPower>(target, 1m, target, null);
        }

        if (HasActiveMonsterHex(MonsterHexKind.ClownCollege)
            && target.IsAlive
            && TryConsumeLimitedProc(_clownCollegeProcsThisTurn, target, 1))
        {
            await PowerCmd.Apply<SlipperyPower>(target, 1m, target, null);
        }

        if (ShouldSuppressDuplicateEnemyThresholdTrigger(target, result, dealer, cardSource))
        {
            return;
        }

        decimal threshold = target.MaxHp * 0.5m;
        bool isBelowThresholdAfterDamage = target.CurrentHp < threshold;
        if (HasActiveMonsterHex(MonsterHexKind.EscapePlan)
            && !_escapePlanTriggered.Contains(combatId)
            && isBelowThresholdAfterDamage)
        {
            _escapePlanTriggered.Add(combatId);
            _escapePlanPending.Add(combatId);
        }

        if (HasActiveMonsterHex(MonsterHexKind.Repulsor)
            && !_repulsorTriggered.Contains(combatId)
            && isBelowThresholdAfterDamage)
        {
            _repulsorTriggered.Add(combatId);
            _repulsorPending.Add(combatId);
        }

        if (HasActiveMonsterHex(MonsterHexKind.DawnbringersResolve)
            && !_dawnTriggered.Contains(combatId)
            && isBelowThresholdAfterDamage)
        {
            _dawnTriggered.Add(combatId);
            int heal = Math.Max(1, (int)Math.Floor(target.MaxHp * 0.25m));
            await CreatureCmd.Heal(target, heal);
        }

        if (HasActiveMonsterHex(MonsterHexKind.FeelTheBurn)
            && isBelowThresholdAfterDamage
            && _feelTheBurnTriggered.Add(combatId))
        {
            _feelTheBurnPending.Add(combatId);
        }

        if (HasActiveMonsterHex(MonsterHexKind.MikaelsBlessing)
            && isBelowThresholdAfterDamage
            && _mikaelsBlessingTriggers.GetValueOrDefault(combatId, 0) < 2)
        {
            _mikaelsBlessingTriggers[combatId] = _mikaelsBlessingTriggers.GetValueOrDefault(combatId, 0) + 1;
            int heal = Math.Max(1, (int)Math.Floor(target.MaxHp * 0.25m));
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

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer?.Side != CombatSide.Enemy || dealer.CombatState?.RunState != RunState || !target.IsAlive)
        {
            return;
        }

        if (HasActiveMonsterHex(MonsterHexKind.ShrinkRay) && result.UnblockedDamage > 0 && target.Side == CombatSide.Player)
        {
            await PowerCmd.Apply<ShrinkPower>(target, 1m, dealer, cardSource);
        }

        if (HasActiveMonsterHex(MonsterHexKind.Firebrand)
            && result.UnblockedDamage > 0
            && target.Side == CombatSide.Player
            && !HextechBurnPower.IsResolvingDamage)
        {
            await PowerCmd.Apply<HextechBurnPower>(target, 2m, dealer, cardSource);
        }

        if (HasActiveMonsterHex(MonsterHexKind.Goldrend)
            && result.UnblockedDamage > 0
            && target.Player != null)
        {
            await HextechGoldrendSync.HandleEnemyGoldrendHit(target.Player);
        }

        if (result.UnblockedDamage <= 0 || target.Side != CombatSide.Player)
        {
            return;
        }

        if (HasActiveMonsterHex(MonsterHexKind.DevilsDance)
            && dealer.IsAlive
            && dealer.CombatId != null
            && _devilsDanceTriggeredThisTurn.Add(dealer.CombatId.Value))
        {
            int heal = Math.Max(1, (int)Math.Floor(dealer.MaxHp * 0.1m));
            await CreatureCmd.Heal(dealer, heal);
        }

        if (HasActiveMonsterHex(MonsterHexKind.SpeedDemon)
            && dealer.IsAlive
            && dealer.CombatId != null)
        {
            _speedDemonPending.Add(dealer.CombatId.Value);
        }

        if (HasActiveMonsterHex(MonsterHexKind.CantTouchThis) && dealer.IsAlive)
        {
            await PowerCmd.Apply<SlipperyPower>(dealer, CantTouchThisSlipperyStacks, dealer, null);
        }

        if (HasActiveMonsterHex(MonsterHexKind.FeyMagic)
            && target.CombatId != null
            && dealer.CombatId != null
            && !_feyMagicPendingNoDrawPlayers.ContainsKey(target.CombatId.Value))
        {
            _feyMagicPendingNoDrawPlayers[target.CombatId.Value] = dealer.CombatId.Value;
        }

        if (HasActiveMonsterHex(MonsterHexKind.FinalForm) && dealer.IsAlive)
        {
            int block = Math.Max(1, (int)Math.Floor(dealer.MaxHp * 0.2m));
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

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
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

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
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
                && TryConsumeLimitedProc(_slapProcsThisTurn, source!, 3))
            {
                await PowerCmd.Apply<StrengthPower>(source!, 1m, source, null);
            }

            if (HasActiveMonsterHex(MonsterHexKind.Tormentor)
                && !_handlingMonsterTormentorBurn
                && TryConsumeLimitedProc(_tormentorProcsThisTurn, source!, 5))
            {
                try
                {
                    _handlingMonsterTormentorBurn = true;
                    await PowerCmd.Apply<HextechBurnPower>(target!, 2m, source, null);
                }
                finally
                {
                    _handlingMonsterTormentorBurn = false;
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
            && TryConsumeLimitedProc(_courageProcsThisTurn, courageSource!, 1))
        {
            int block = Math.Max(1, (int)Math.Floor(courageSource!.MaxHp * CourageOfColossusBlockPercent));
            await CreatureCmd.GainBlock(courageSource, block, ValueProp.Unpowered, null);
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
            || !HextechMonsterInteractionPolicy.IsTrueCombatDeath(target, out CombatState? combatState))
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
