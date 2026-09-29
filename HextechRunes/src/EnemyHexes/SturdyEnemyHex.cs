namespace HextechRunes;

internal sealed class SturdyEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Sturdy;

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		foreach (Creature enemy in enemies)
		{
			decimal percent = enemy.CurrentHp * 2 < enemy.MaxHp ? 0.04m : 0.02m;
			int heal = HextechEnemyHexContext.FractionOfMaxHp(enemy, percent);
			if (heal > 0)
			{
				await CreatureCmd.Heal(enemy, heal);
			}
		}
	}
}
