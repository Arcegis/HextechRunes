namespace HextechRunes;

// 文案"每 N 回合"= 第 N、2N、3N… 回合触发（与英文 "Every N turns" 一致），第 1 回合不触发，
// 所以 N=1 是从第 2 回合起每回合都触发。额外回合不推进 RoundNumber、回合钩子会重入，
// 调用方仍需按回合号防重。
internal static class HextechRoundInterval
{
	/// <summary>
	/// 同一回合号只认领一次：额外回合不推进 RoundNumber、回合钩子会重入，按回合号防重的效果用它记账。
	/// 认领成功时把 <paramref name="lastClaimedRound"/> 更新为本回合号；放在判定链最后，只在其他条件都满足时认领。
	/// </summary>
	internal static bool TryClaimRound(ref int lastClaimedRound, int roundNumber)
	{
		if (lastClaimedRound == roundNumber)
		{
			return false;
		}

		lastClaimedRound = roundNumber;
		return true;
	}

	internal static bool IsDue(int roundNumber, int everyNRounds)
	{
		return everyNRounds > 0
			&& roundNumber > 1
			&& roundNumber % everyNRounds == 0;
	}
}
