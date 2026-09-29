namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	public override async Task BeforeCombatStart()
	{
		HextechCombatHooks.ResetTransientCombatState();
		HextechGoldrendSync.BeginCombat(RunState);
		ResetCombatTracking();
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

		// HP 归一化、持久海克斯和开局效果先完成共享状态写入，再刷新本机 UI。
		// Refresh 自行隔离表现层异常，避免它中断同步流程。
		HextechEnemyUi.Refresh(this);
	}

	public override async Task AfterCombatEnd(CombatRoom room)
	{
		HextechCombatHooks.ResetTransientCombatState();
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

		if (HextechPlayerContextHelper.IsNetworkMultiplayerRun())
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
			await ApplyPersistentMonsterHexes(enemy);
		}

		HextechEnemyUi.Refresh(this);
	}

	public override async Task BeforeSideTurnStartForParticipants(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, HextechCombatState combatState)
	{
		HextechCombatHooks.ClearPendingManualPlayState();
		await HextechEnemyHexDispatcher.ForEachActive(
			this,
			participants,
			(effect, context) => effect.BeforeSideTurnStart(context, choiceContext, side, combatState));

		IReadOnlyList<Creature> players = HextechCombatCreatureHelper.GetAlivePlayerSideCreatures(combatState);

		if (side == CombatSide.Player)
		{
			await BeforePlayerSideTurnStart(combatState, players, HextechEnemyHexContext.FilterTakingTurn(players, participants), participants);
			return;
		}

		if (side == CombatSide.Enemy)
		{
			await BeforeEnemySideTurnStart(combatState, players);
		}
	}
}
