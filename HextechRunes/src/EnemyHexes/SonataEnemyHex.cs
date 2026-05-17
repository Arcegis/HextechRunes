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
			int block = Math.Max(1, (int)Math.Floor(enemy.MaxHp * 0.1m));
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
			int heal = Math.Max(1, (int)Math.Floor(enemy.MaxHp * 0.05m));
			await CreatureCmd.Heal(enemy, heal);
		}
	}
}
