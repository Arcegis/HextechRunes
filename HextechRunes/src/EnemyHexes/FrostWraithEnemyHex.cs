namespace HextechRunes;

internal sealed class FrostWraithEnemyHex : HextechEnemyHexEffect
{
	internal const int TurnsNeeded = 3;
	internal const int TemporarySlowAmount = 50;

	internal override MonsterHexKind Kind => MonsterHexKind.FrostWraith;

	internal override async Task BeforeEnemySideTurnStart(
		HextechEnemyHexContext context,
		HextechCombatState combatState,
		IReadOnlyList<Creature> players,
		IReadOnlyList<Creature> enemies)
	{
		// 临时缓慢在玩家回合开始时清除，因此必须在敌方回合开始时施加。
		// 没有玩家时不消耗本回合的防重次数。
		if (players.Count > 0
			&& context.TryConsumeRoundInterval(Kind, combatState, TurnsNeeded))
		{
			await PowerCmd.Apply<HextechTemporarySlowPower>(players, TemporarySlowAmount, null, null);
		}
	}

	internal static bool ShouldTriggerForRound(int roundNumber)
	{
		return HextechRoundInterval.IsDue(roundNumber, TurnsNeeded);
	}
}
