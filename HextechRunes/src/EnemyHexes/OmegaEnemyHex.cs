namespace HextechRunes;

internal sealed class OmegaEnemyHex : HextechEnemyHexEffect
{
	private const string TriggerKey = "enemy-omega-disintegration";

	internal override MonsterHexKind Kind => MonsterHexKind.Omega;

	internal override async Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		if (combatState.RoundNumber != 4
			|| players.Count == 0
			|| context.Tracking.PlayerRuneProcsThisCombat.ContainsKey(TriggerKey))
		{
			return;
		}

		context.Tracking.PlayerRuneProcsThisCombat[TriggerKey] = 1;
		await context.RunGroupedPlayerDebuffBurst(async () =>
		{
			await PowerCmd.Apply<DisintegrationPower>(players, context.TierValue(Kind, 5, 8, 12), null, null);
		});
	}
}
