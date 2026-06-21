using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	public override async Task BeforeCombatStart()
	{
		HextechGoldrendSync.ResetCombat();
		ResetCombatTracking();
		HextechEnemyUi.Refresh(this);
		HextechMultiplayerScalingCompat.RefreshHostScalingFlagForLocalHost(this);
		if (RunState.CurrentRoom is CombatRoom currentCombatRoom)
		{
			await HextechMultiplayerScalingCompat.NormalizeCombatEnemyHpIfNeeded(this, currentCombatRoom);
		}

		await ApplyToCurrentEnemiesIfNeeded();

		if (RunState.CurrentRoom is CombatRoom combatRoom)
		{
			await ApplyCombatStartEnemyHexes(combatRoom);
		}

		_combatTracking.EnemyProtectiveVeilTurnCounter = 0;
	}

	public override async Task AfterCombatEnd(CombatRoom room)
	{
		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, context) => effect.AfterCombatEnd(context, room));

		await HextechGoldrendSync.ApplyPendingCombatGoldLosses(RunState);
		ResetCombatTracking();
	}

	public override async Task AfterCombatVictory(CombatRoom room)
	{
		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, context) => effect.AfterCombatVictory(context, room));

		if (HextechRelicBase.IsNetworkMultiplayerRun())
		{
			await ApplySharedCombatVictoryRunes(room);
		}
	}

	private async Task ApplySharedCombatVictoryRunes(CombatRoom room)
	{
		foreach (IHextechSharedCombatVictoryRune rune in RunState.Players
			.SelectMany(static player => player.Relics)
			.OfType<IHextechSharedCombatVictoryRune>())
		{
			await rune.ApplySharedCombatVictory(room);
		}
	}

	public override async Task AfterCreatureAddedToCombat(Creature creature)
	{
		if (creature.Side != CombatSide.Enemy || !creature.IsAlive)
		{
			return;
		}

		await HextechMultiplayerScalingCompat.NormalizeEnemyHpIfNeeded(this, creature);

		if (RunState.CurrentRoom is CombatRoom combatRoom
			&& await TryApplyDeferredBossStartHexes(creature, combatRoom))
		{
			HextechEnemyUi.Refresh(this);
			return;
		}

		if (ShouldDeferInitialBossStartHexes(creature))
		{
			HextechEnemyUi.Refresh(this);
			return;
		}

		await ApplyPersistentMonsterHexes(creature);
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
			if (ShouldDeferInitialBossStartHexes(enemy))
			{
				continue;
			}

			await ApplyPersistentMonsterHexes(enemy);
		}

		HextechEnemyUi.Refresh(this);
	}

	public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		await ApplyDeferredBossStartHexes(combatState);
		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			(effect, context) => effect.BeforeSideTurnStart(context, choiceContext, side, combatState));

		IReadOnlyList<Creature> players = HextechCombatCreatureHelper.GetAlivePlayerSideCreatures(combatState);

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
