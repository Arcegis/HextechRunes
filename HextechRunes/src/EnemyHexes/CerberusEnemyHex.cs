namespace HextechRunes;

internal sealed class CerberusEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Cerberus;

	internal override async Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		if (combatState.RunState != context.RunState)
		{
			return;
		}

		List<Creature> enemies = context.GetAliveEnemies(combatState).ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		await PowerCmd.Apply<VigorPower>(enemies, context.TierValue(Kind, 2, 3, 4), null, null);
	}
}
