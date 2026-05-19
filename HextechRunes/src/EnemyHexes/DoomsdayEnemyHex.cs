namespace HextechRunes;

internal sealed class DoomsdayEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Doomsday;

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		if (players.Count == 0)
		{
			return;
		}

		await context.RunGroupedPlayerDebuffBurst(async () =>
		{
			await PowerCmd.Apply<DisintegrationPower>(players, context.TierValue(Kind, 1, 2, 3), null, null);
		});
	}
}
