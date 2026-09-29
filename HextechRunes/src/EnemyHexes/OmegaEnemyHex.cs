namespace HextechRunes;

internal sealed class OmegaEnemyHex : HextechEnemyHexEffect
{
	private const string TriggerKey = "enemy-omega-disintegration";

	internal override MonsterHexKind Kind => MonsterHexKind.Omega;

	internal override async Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		// 全场一次的全局计次（额外回合会重入回合开始钩子）。
		if (combatState.RoundNumber != 4
			|| players.Count == 0
			|| HextechCombatProcTracker.ConsumeGlobalProcInCombat(context.Tracking, TriggerKey) > 0)
		{
			return;
		}

		await PowerCmd.Apply<DisintegrationPower>(players, context.TierValue(Kind, 5, 8, 12), null, null);
	}
}
