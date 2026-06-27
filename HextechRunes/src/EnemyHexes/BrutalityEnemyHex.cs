namespace HextechRunes;

internal sealed class BrutalityEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Brutality;

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		int vigor = context.TierValue(Kind, 1, 2, 3);
		foreach (Creature enemy in enemies)
		{
			if (!enemy.IsAlive)
			{
				continue;
			}

			// 失去 5% 最大生命(非伤害,不会击杀,至少保留 1 点),换取活力。
			int loss = Math.Max(1, (int)Math.Floor(enemy.MaxHp * 0.05m));
			int newHp = Math.Max(1, enemy.CurrentHp - loss);
			if (newHp < enemy.CurrentHp)
			{
				await CreatureCmd.SetCurrentHp(enemy, newHp);
			}

			await PowerCmd.Apply<VigorPower>(enemy, vigor, enemy, null);
		}
	}
}
