namespace HextechRunes;

internal sealed class SonataEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Sonata;

	internal override async Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		if (combatState.RoundNumber % 2 != 1)
		{
			return;
		}

		foreach (Creature enemy in context.GetAliveEnemies(combatState))
		{
			decimal blockPercent = context.TierValue(Kind, 0.08m, 0.10m, 0.12m);
			int block = Math.Max(1, (int)Math.Floor(enemy.MaxHp * blockPercent));
			await CreatureCmd.GainBlock(enemy, block, ValueProp.Unpowered, null);
		}
	}

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		if (combatState.RoundNumber % 2 != 0)
		{
			return;
		}

		foreach (Creature enemy in enemies)
		{
			decimal healPercent = context.TierValue(Kind, 0.04m, 0.05m, 0.06m);
			int heal = Math.Max(1, (int)Math.Floor(enemy.MaxHp * healPercent));
			await CreatureCmd.Heal(enemy, heal);
		}
	}
}
