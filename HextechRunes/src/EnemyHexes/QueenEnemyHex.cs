namespace HextechRunes;

internal sealed class QueenEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Queen;

	internal override async Task ApplyCombatStartPlayerDebuffs(HextechEnemyHexContext context, CombatRoom room, IReadOnlyList<Creature> players)
	{
		await context.RunGroupedPlayerDebuffBurst(async () =>
		{
			await PowerCmd.Apply<ChainsOfBindingPower>(players, 1m, null, null);
		});
	}

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		if (combatState.RoundNumber <= 1 || combatState.RoundNumber % 2 != 0)
		{
			return;
		}

		IReadOnlyList<Creature> queenTargets = players
			.Where(player => player.GetPowerAmount<ChainsOfBindingPower>() < 3m)
			.ToList();
		if (queenTargets.Count > 0)
		{
			await context.RunGroupedPlayerDebuffBurst(async () =>
			{
				await PowerCmd.Apply<ChainsOfBindingPower>(queenTargets, 1m, null, null);
			});
		}
	}
}
