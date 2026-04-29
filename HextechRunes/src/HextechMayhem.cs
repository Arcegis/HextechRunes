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
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier : ModifierModel
{
    public override async Task AfterActEntered()
    {
        int actIndex = RunState.CurrentActIndex;
        if (!IsActResolved(actIndex) && TryRecoverResolvedActsFromPlayerRelics(nameof(AfterActEntered)))
        {
            HextechEnemyUi.Refresh(this);
        }

        if (actIndex <= 0 || actIndex > 2 || IsActResolved(actIndex))
        {
            return;
        }

        Log.Info($"[{ModInfo.Id}][Mayhem] AfterActEntered: resolving act selection before first room actIndex={actIndex}");
        await HextechRuneSelectionCoordinator.HandleActSelection(RunState, this);
    }

    public override async Task BeforeRoomEntered(AbstractRoom room)
    {
        int actIndex = RunState.CurrentActIndex;
        if (!IsActResolved(actIndex) && TryRecoverResolvedActsFromPlayerRelics(nameof(BeforeRoomEntered)))
        {
            HextechEnemyUi.Refresh(this);
        }

        if (actIndex < 0 || actIndex > 2 || IsActResolved(actIndex) || room is EventRoom or MapRoom)
        {
            return;
        }

        if (actIndex == 0)
        {
            Log.Warn($"[{ModInfo.Id}][Mayhem] BeforeRoomEntered: skipping unsafe act0 selection before room={room.GetType().Name}; waiting for post-Neow or map path");
            return;
        }

        Log.Info($"[{ModInfo.Id}][Mayhem] BeforeRoomEntered: resolving pending act selection before room={room.GetType().Name} actIndex={actIndex}");
        await HextechRuneSelectionCoordinator.HandleActSelection(RunState, this);
    }

    public override async Task BeforeCombatStart()
    {
        HextechGoldrendSync.ResetCombat();
        ResetCombatTracking();
        HextechEnemyUi.Refresh(this);
        await ApplyToCurrentEnemiesIfNeeded();

        if (HasActiveMonsterHex(MonsterHexKind.Queen)
            && RunState.CurrentRoom is CombatRoom combatRoom)
        {
            IReadOnlyList<Creature> players = GetAlivePlayerSideCreatures(combatRoom.CombatState);
            if (players.Count > 0)
            {
                await RunGroupedPlayerDebuffBurst(async () =>
                {
                    await PowerCmd.Apply<ChainsOfBindingPower>(players, 1m, null, null);
                });
            }
        }

        if (HasActiveMonsterHex(MonsterHexKind.HandOfBaron)
            && RunState.CurrentRoom is CombatRoom combatRoomForBaron)
        {
            IReadOnlyList<Creature> players = GetAlivePlayerSideCreatures(combatRoomForBaron.CombatState);
            if (players.Count > 0)
            {
                await RunGroupedPlayerDebuffBurst(async () =>
                {
                    await PowerCmd.Apply<ShrinkPower>(players, 99m, null, null);
                });
            }
        }

        _enemyProtectiveVeilTurnCounter = 0;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await HextechGoldrendSync.ApplyPendingCombatGoldLosses(RunState);
        ResetCombatTracking();
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        return Task.CompletedTask;
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (!HasActiveMonsterHex(MonsterHexKind.CuttingEdgeAlchemist)
            || room is not CombatRoom
            || rewards.Count == 0)
        {
            return false;
        }

        bool modified = false;
        for (int i = 0; i < rewards.Count; i++)
        {
			if (rewards[i] is not PotionReward potionReward
				|| potionReward.Potion?.Rarity == PotionRarity.Common
				|| !TryCreateCommonPotionReward(player, out PotionReward? replacement)
				|| replacement == null)
			{
				continue;
			}

            rewards[i] = replacement;
            modified = true;
        }

        return modified;
    }

    private static bool TryCreateCommonPotionReward(Player player, out PotionReward? reward)
    {
        List<PotionModel> candidates = PotionFactory.GetPotionOptions(player, Array.Empty<PotionModel>())
            .Where(static potion => potion.Rarity == PotionRarity.Common)
            .ToList();
        if (candidates.Count == 0)
        {
            reward = null;
            return false;
        }

        PotionModel potion = candidates[player.PlayerRng.Rewards.NextInt(candidates.Count)].ToMutable();
        reward = new PotionReward(potion, player);
        return true;
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (creature.Side != CombatSide.Enemy || !creature.IsAlive)
        {
            return;
        }

        await ApplyPersistentMonsterHexes(creature);
        await TryApplyServantMasterIllusion(creature, creature, null);
        HextechEnemyUi.Refresh(this);
    }

    public async Task ApplyToCurrentEnemiesIfNeeded()
    {
        if (RunState.CurrentRoom is not CombatRoom combatRoom)
        {
            return;
        }

        foreach (Creature enemy in combatRoom.CombatState.Enemies.Where(static creature => creature.IsAlive))
        {
            await ApplyPersistentMonsterHexes(enemy);
        }

        HextechEnemyUi.Refresh(this);
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        await NormalizeEnemyPainfulStabsPowers(combatState);

        IReadOnlyList<Creature> players = GetAlivePlayerSideCreatures(combatState);

        if (side == CombatSide.Player)
        {
            await BeforePlayerSideTurnStart(combatState, players);
            return;
        }

        if (side == CombatSide.Enemy)
        {
            await BeforeEnemySideTurnStart(combatState, players);
        }
    }

}
