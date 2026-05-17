namespace HextechRunes;

internal sealed class UnmovableMountainEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.UnmovableMountain;

	internal override int PersistentOrder => 100;

	internal override async Task ApplyPersistentToEnemy(HextechEnemyHexContext context, Creature creature, int? maxHpBaseOverride, bool replayOneShotPowers)
	{
		if (HextechMayhemModifier.TryMarkPersistentHexApplied(context.Tracking.UnmovableMountainApplied, creature, replayOneShotPowers))
		{
			await PowerCmd.Apply<BarricadePower>(creature, 1m, creature, null);
		}
	}

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		foreach (Creature enemy in enemies)
		{
			if (enemy.Block <= 0)
			{
				int block = Math.Max(1, (int)Math.Floor(enemy.MaxHp * 0.08m));
				await CreatureCmd.GainBlock(enemy, block, ValueProp.Unpowered, null);
			}
		}
	}
}
