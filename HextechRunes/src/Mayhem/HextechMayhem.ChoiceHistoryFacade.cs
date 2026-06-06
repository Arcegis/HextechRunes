using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	public IReadOnlyList<HextechTelemetry.RuneChoiceRecord> GetTelemetryChoiceRecords()
	{
		return _choiceHistory.GetTelemetryChoiceRecords();
	}

	public void RecordTelemetryChoice(HextechTelemetry.RuneChoiceRecord record)
	{
		_choiceHistory.RecordTelemetryChoice(record);
	}

	public HashSet<ModelId> GetSeenPlayerRuneIds(Player player)
	{
		return _choiceHistory.GetSeenPlayerRuneIds(player, RunState);
	}

	public void RecordSeenPlayerRunes(Player player, IEnumerable<RelicModel> relics)
	{
		_choiceHistory.RecordSeenPlayerRunes(player, relics, RunState);
	}

	private int GetHighestActResolvedByTelemetryChoices(int maxActIndex)
	{
		int lastActIndex = _actState.LastActIndexFor(maxActIndex);
		if (lastActIndex < 0 || RunState.Players.Count == 0)
		{
			return -1;
		}

		IReadOnlyList<HextechTelemetry.RuneChoiceRecord> records = GetTelemetryChoiceRecords();
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
			for (int playerSlot = 0; playerSlot < RunState.Players.Count; playerSlot++)
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

	private int GetHighestActResolvedByPlayerRuneCounts(int maxActIndex)
	{
		int lastActIndex = _actState.LastActIndexFor(maxActIndex);
		if (lastActIndex < 0)
		{
			return -1;
		}

		int minHexCount = int.MaxValue;
		foreach (Player player in RunState.Players)
		{
			int count = player.Relics.Count(HextechCatalog.IsHextechRelic);
			minHexCount = Math.Min(minHexCount, count);
		}

		if (minHexCount == int.MaxValue)
		{
			return -1;
		}

		int loopHexCount = Math.Max(0, minHexCount - _hexCountRecoveryBaseline);
		if (loopHexCount <= 0)
		{
			return -1;
		}

		return Math.Min(lastActIndex, loopHexCount - 1);
	}

	private bool TryInferRarityForAct(int actIndex, out HextechRarityTier rarity)
	{
		return TryInferRarityForActFromTelemetryChoices(actIndex, out rarity)
			|| TryInferRarityForActFromPlayerRelics(actIndex, out rarity);
	}

	private bool TryInferRarityForActFromTelemetryChoices(int actIndex, out HextechRarityTier rarity)
	{
		foreach (HextechTelemetry.RuneChoiceRecord record in GetTelemetryChoiceRecords().Where(record => record.ActIndex == actIndex))
		{
			if (Enum.TryParse(record.Rarity, ignoreCase: true, out rarity))
			{
				return true;
			}
		}

		rarity = default;
		return false;
	}

	private bool TryInferRarityForActFromPlayerRelics(int actIndex, out HextechRarityTier rarity)
	{
		foreach (Player player in RunState.Players)
		{
			RelicModel? relic = player.Relics
				.Where(HextechCatalog.IsHextechRelic)
				.ElementAtOrDefault(_hexCountRecoveryBaseline + actIndex);
			if (HextechCatalog.TryGetPlayerRuneRarity(relic, out rarity))
			{
				return true;
			}
		}

		rarity = default;
		return false;
	}

	private string DescribePlayerHexCounts()
	{
		return HextechMayhemActRecovery.DescribePlayerHexCounts(RunState);
	}

	private string DescribeTelemetryChoiceCounts()
	{
		return HextechMayhemActRecovery.DescribeTelemetryChoiceCounts(_choiceHistory);
	}
}
