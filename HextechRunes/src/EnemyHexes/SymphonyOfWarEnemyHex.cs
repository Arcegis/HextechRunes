namespace HextechRunes;

internal sealed class SymphonyOfWarEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.SymphonyOfWar;

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		int ritual = context.TierValue(Kind, 0, 1, 2);
		if (ritual <= 0)
		{
			return;
		}

		foreach (Creature enemy in enemies)
		{
			if (enemy.IsAlive)
			{
				await PowerCmd.Apply<RitualPower>(enemy, ritual, enemy, null);
			}
		}
	}
}
