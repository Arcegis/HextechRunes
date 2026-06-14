using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static class HextechMayhemActRecovery
{
	public static HextechMayhemActRecoveryResult RecoverResolvedActs(
		RunState runState,
		HextechMayhemActState actState,
		HextechMayhemChoiceHistoryState choiceHistory,
		int hexCountRecoveryBaseline)
	{
		int currentActIndex = Math.Min(runState.CurrentActIndex, actState.ActCount - 1);
		if (currentActIndex < 0 || runState.Players.Count == 0)
		{
			return HextechMayhemActRecoveryResult.None;
		}

		int telemetryRecoverThroughAct = GetHighestActResolvedByTelemetryChoices(runState, actState, choiceHistory, currentActIndex);
		int countRecoverThroughAct = GetHighestActResolvedByPlayerRuneCounts(
			runState,
			actState,
			currentActIndex == 0 ? 0 : currentActIndex - 1,
			hexCountRecoveryBaseline);
		int recoverThroughAct = Math.Max(telemetryRecoverThroughAct, countRecoverThroughAct);
		if (recoverThroughAct < 0)
		{
			return HextechMayhemActRecoveryResult.None;
		}

		bool changed = false;
		for (int actIndex = 0; actIndex <= recoverThroughAct; actIndex++)
		{
			changed |= actState.TryMarkResolved(actIndex);

			if (TryInferRarityForAct(runState, actState, choiceHistory, hexCountRecoveryBaseline, actIndex, out HextechRarityTier rarity))
			{
				changed |= actState.TrySetRarityIfMissing(actIndex, rarity);
			}
		}

		return new HextechMayhemActRecoveryResult(changed, recoverThroughAct, telemetryRecoverThroughAct, countRecoverThroughAct);
	}

	public static string DescribePlayerHexCounts(RunState runState)
	{
		return string.Join(",", runState.Players.Select(player => $"{player.NetId}:{player.Relics.Count(HextechCatalog.IsHextechRelic)}"));
	}

	public static string DescribeTelemetryChoiceCounts(HextechMayhemChoiceHistoryState choiceHistory)
	{
		return string.Join(",", choiceHistory.GetTelemetryChoiceRecords()
			.GroupBy(static record => record.ActIndex)
			.OrderBy(static group => group.Key)
			.Select(static group => $"{group.Key}:{group.Count()}"));
	}

	public static int GetMinimumPlayerHexCount(RunState runState)
	{
		int minHexCount = int.MaxValue;
		foreach (Player player in runState.Players)
		{
			int count = player.Relics.Count(HextechCatalog.IsHextechRelic);
			minHexCount = Math.Min(minHexCount, count);
		}

		return minHexCount == int.MaxValue ? 0 : minHexCount;
	}

	private static int GetHighestActResolvedByTelemetryChoices(
		RunState runState,
		HextechMayhemActState actState,
		HextechMayhemChoiceHistoryState choiceHistory,
		int maxActIndex)
	{
		int lastActIndex = actState.LastActIndexFor(maxActIndex);
		if (lastActIndex < 0 || runState.Players.Count == 0)
		{
			return -1;
		}

		IReadOnlyList<HextechTelemetry.RuneChoiceRecord> records = choiceHistory.GetTelemetryChoiceRecords();
		if (records.Count == 0)
		{
			return -1;
		}

		int highest = -1;
		for (int actIndex = 0; actIndex <= lastActIndex; actIndex++)
		{
			HashSet<int> playerSlots = records
				.Where(record => record.ActIndex == actIndex)
				.Select(static record => record.PlayerSlot)
				.ToHashSet();
			bool allPlayersRecorded = true;
			for (int playerSlot = 0; playerSlot < runState.Players.Count; playerSlot++)
			{
				if (!playerSlots.Contains(playerSlot))
				{
					allPlayersRecorded = false;
					break;
				}
			}

			if (!allPlayersRecorded)
			{
				break;
			}

			highest = actIndex;
		}

		return highest;
	}

	private static int GetHighestActResolvedByPlayerRuneCounts(
		RunState runState,
		HextechMayhemActState actState,
		int maxActIndex,
		int hexCountRecoveryBaseline)
	{
		int lastActIndex = actState.LastActIndexFor(maxActIndex);
		if (lastActIndex < 0)
		{
			return -1;
		}

		int minHexCount = GetMinimumPlayerHexCount(runState);
		int loopHexCount = Math.Max(0, minHexCount - hexCountRecoveryBaseline);
		if (loopHexCount <= 0)
		{
			return -1;
		}

		return Math.Min(lastActIndex, loopHexCount - 1);
	}

	private static bool TryInferRarityForAct(
		RunState runState,
		HextechMayhemActState actState,
		HextechMayhemChoiceHistoryState choiceHistory,
		int hexCountRecoveryBaseline,
		int actIndex,
		out HextechRarityTier rarity)
	{
		_ = actState;
		return TryInferRarityForActFromTelemetryChoices(choiceHistory, actIndex, out rarity)
			|| TryInferRarityForActFromPlayerRelics(runState, hexCountRecoveryBaseline, actIndex, out rarity);
	}

	private static bool TryInferRarityForActFromTelemetryChoices(
		HextechMayhemChoiceHistoryState choiceHistory,
		int actIndex,
		out HextechRarityTier rarity)
	{
		foreach (HextechTelemetry.RuneChoiceRecord record in choiceHistory.GetTelemetryChoiceRecords().Where(record => record.ActIndex == actIndex))
		{
			if (Enum.TryParse(record.Rarity, ignoreCase: true, out rarity))
			{
				return true;
			}
		}

		rarity = default;
		return false;
	}

	private static bool TryInferRarityForActFromPlayerRelics(
		RunState runState,
		int hexCountRecoveryBaseline,
		int actIndex,
		out HextechRarityTier rarity)
	{
		foreach (Player player in runState.Players)
		{
			RelicModel? relic = player.Relics
				.Where(HextechCatalog.IsHextechRelic)
				.ElementAtOrDefault(hexCountRecoveryBaseline + actIndex);
			if (HextechCatalog.TryGetPlayerRuneRarity(relic, out rarity))
			{
				return true;
			}
		}

		rarity = default;
		return false;
	}
}

internal readonly record struct HextechMayhemActRecoveryResult(
	bool Changed,
	int RecoverThroughAct,
	int TelemetryRecoverThroughAct,
	int CountRecoverThroughAct)
{
	public static HextechMayhemActRecoveryResult None => new(false, -1, -1, -1);
}
