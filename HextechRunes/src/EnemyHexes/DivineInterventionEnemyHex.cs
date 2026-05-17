namespace HextechRunes;

internal sealed class DivineInterventionEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.DivineIntervention;

	internal override Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		IReadOnlyList<Creature> aliveEnemies = context.GetAliveEnemies(combatState);
		return combatState.RoundNumber > 1 && combatState.RoundNumber % 4 == 0 && aliveEnemies.Count > 0
			? PowerCmd.Apply<IntangiblePower>(aliveEnemies, 1m, null, null)
			: Task.CompletedTask;
	}
}
