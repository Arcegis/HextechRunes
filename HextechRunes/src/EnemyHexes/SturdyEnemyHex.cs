namespace HextechRunes;

internal sealed class SturdyEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Sturdy;

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		foreach (Creature enemy in enemies)
		{
			decimal percent = enemy.CurrentHp * 2 < enemy.MaxHp ? 0.04m : 0.02m;
			int maxHeal = context.TierValue(Kind, 10, 15, 20);
			int heal = Math.Min(maxHeal, Math.Max(1, (int)Math.Floor(enemy.MaxHp * percent)));
			if (heal > 0)
			{
				await CreatureCmd.Heal(enemy, heal);
			}
		}
	}
}
