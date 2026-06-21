namespace HextechRunes;

internal sealed class CerberusEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Cerberus;

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		if (combatState.RunState != context.RunState || enemies.Count == 0)
		{
			return;
		}

		await PowerCmd.Apply<VigorPower>(enemies, context.TierValue(Kind, 1, 2, 3), null, null);
	}
}
