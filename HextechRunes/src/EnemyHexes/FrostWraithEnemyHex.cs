namespace HextechRunes;

internal sealed class FrostWraithEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.FrostWraith;

#if STS2_104_OR_NEWER
	internal override async Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		if (combatState.RoundNumber > 1 && combatState.RoundNumber % 4 == 0 && players.Count > 0)
		{
			await context.RunGroupedPlayerDebuffBurst(async () =>
			{
				await PowerCmd.Apply<BorrowedTimePower>(players, 1m, null, null);
			});
		}
	}
#else
	internal override Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		return Task.CompletedTask;
	}
#endif
}
