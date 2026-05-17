namespace HextechRunes;

internal sealed class HandOfBaronEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.HandOfBaron;

	internal override decimal ModifyDamageMultiplicative(HextechEnemyHexContext context, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return 1.1m;
	}

	internal override async Task ApplyCombatStartPlayerDebuffs(HextechEnemyHexContext context, CombatRoom room, IReadOnlyList<Creature> players)
	{
		await context.RunGroupedPlayerDebuffBurst(async () =>
		{
			await PowerCmd.Apply<ShrinkPower>(players, 99m, null, null);
		});
	}

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		if (players.Count == 0)
		{
			return;
		}

		await context.RunGroupedPlayerDebuffBurst(async () =>
		{
			await PowerCmd.Apply<ShrinkPower>(players, 2m, null, null);
		});
	}
}
