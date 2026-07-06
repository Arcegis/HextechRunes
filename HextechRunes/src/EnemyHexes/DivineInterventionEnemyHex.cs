namespace HextechRunes;

internal sealed class DivineInterventionEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.DivineIntervention;

	internal override Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		// 额外回合不推进 RoundNumber 且回合开始 hook 会重入,按回合防重。
		if (combatState.RoundNumber <= 1
			|| combatState.RoundNumber % 4 != 0
			|| HextechCombatProcTracker.ConsumeGlobalProcInCombat(context.Tracking, $"round-once:{Kind}:{combatState.RoundNumber}") > 0)
		{
			return Task.CompletedTask;
		}

		IReadOnlyList<Creature> aliveEnemies = context.GetAliveEnemies(combatState);
		return aliveEnemies.Count > 0
			? PowerCmd.Apply<IntangiblePower>(aliveEnemies, 1m, null, null)
			: Task.CompletedTask;
	}
}
