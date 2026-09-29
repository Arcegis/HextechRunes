using MegaCrit.Sts2.Core.GameActions;

namespace HextechRunes;

internal static partial class HextechChoiceCodec
{
	private static readonly Lazy<IReadOnlyList<ModelId>> PlayerRuneIdsByOrdinal = new(
		() => HextechCatalog.GetConfigurablePlayerRuneIds()
			.OrderBy(static id => id.Category, StringComparer.Ordinal)
			.ThenBy(static id => id.Entry, StringComparer.Ordinal)
			.ToArray());

	public static PlayerChoiceResult CreateRuneSelection(int actIndex, int choiceOrdinal, int selectedIndex, IReadOnlyList<int> rerollHistory, IReadOnlyList<RelicModel> finalOptions, IEnumerable<ModelId>? seenOptionIds = null)
	{
		List<int> payload = [ Magic, ChoiceKindRuneSelection, actIndex, choiceOrdinal, selectedIndex, rerollHistory.Count ];
		payload.AddRange(rerollHistory);
		ModelId[] finalIds = finalOptions.Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id).ToArray();
		HextechStableModelIdListCodec.Append(payload, finalIds);
		HextechRuneSeenHistoryCodec.Append(payload, (seenOptionIds ?? []).Concat(finalIds));
		HextechRuneWeightCodec.Append(payload, finalOptions);
		HextechGeneratedRuneDataCodec.Append(payload, finalOptions);

		return PlayerChoiceResult.FromIndexes(payload);
	}

	public static bool IsRuneSelection(PlayerChoiceResult result)
	{
		return TryGetIndexPayload(result, out List<int> payload)
			&& payload.Count >= 2
			&& payload[0] == Magic
			&& payload[1] == ChoiceKindRuneSelection;
	}

	public static bool IsRuneSelection(PlayerChoiceResult result, int expectedActIndex, int expectedChoiceOrdinal)
	{
		return TryDecodeRuneSelection(result, expectedActIndex, expectedChoiceOrdinal, out _, out _, out _);
	}

	public static bool TryDecodeRuneSelection(
		PlayerChoiceResult result,
		int expectedActIndex,
		int expectedChoiceOrdinal,
		out int selectedIndex,
		out List<int> rerollHistory,
		out List<ModelId> finalOptionIds)
	{
		return TryDecodeRuneSelection(result, expectedActIndex, expectedChoiceOrdinal,
			out selectedIndex, out rerollHistory, out finalOptionIds, out _);
	}

	public static bool TryDecodeRuneSelection(
		PlayerChoiceResult result,
		int expectedActIndex,
		int expectedChoiceOrdinal,
		out int selectedIndex,
		out List<int> rerollHistory,
		out List<ModelId> finalOptionIds,
		out List<ModelId> seenOptionIds)
	{
		selectedIndex = -1;
		rerollHistory = [];
		finalOptionIds = [];
		seenOptionIds = [];
		if (!TryGetIndexPayload(result, out List<int> payload)
			|| payload.Count < 6
			|| payload[0] != Magic
			|| payload[1] != ChoiceKindRuneSelection
			|| payload[2] != expectedActIndex
			|| payload[3] != expectedChoiceOrdinal)
		{
			return false;
		}

		selectedIndex = payload[4];
		int rerollCount = payload[5];
		const int headerCount = 6;
		// 无限重随可超过单次候选列表的 64 项限制，历史长度由实际载荷约束。
		if (rerollCount < 0
			|| !HasRemaining(payload, headerCount, rerollCount))
		{
			return false;
		}

		rerollHistory = payload.Skip(headerCount).Take(rerollCount).ToList();
		int cursor = rerollCount + headerCount;
		return TryDecodeRuneSelectionFinalOptions(payload, cursor, out finalOptionIds, out seenOptionIds);
	}

	private static bool TryDecodeRuneSelectionFinalOptions(List<int> payload, int cursor, out List<ModelId> finalOptionIds, out List<ModelId> seenOptionIds)
	{
		finalOptionIds = [];
		seenOptionIds = [];
		if (payload.Count <= cursor)
		{
			return false;
		}

		if (payload[cursor] == HextechStableModelIdListCodec.Version)
		{
			return HextechStableModelIdListCodec.TryDecode(payload, cursor, out finalOptionIds, out int nextCursor)
				&& HextechRuneSeenHistoryCodec.TryRead(payload, ref nextCursor, out seenOptionIds)
				&& finalOptionIds.All(seenOptionIds.Contains)
				&& HextechRuneWeightCodec.TryRead(payload, ref nextCursor, out _)
				&& HextechGeneratedRuneDataCodec.TryDecode(payload, nextCursor, finalOptionIds.Count, out _);
		}

		int optionCount = payload[cursor];
		cursor++;
		if (optionCount < 0
			|| optionCount > MaxChoiceListCount
			|| !HasRemaining(payload, cursor, optionCount))
		{
			return false;
		}

		for (int i = 0; i < optionCount; i++)
		{
			if (!TryGetRuneIdForOrdinal(payload[cursor + i], out ModelId id))
			{
				finalOptionIds.Clear();
				return false;
			}

			finalOptionIds.Add(id);
		}

		cursor += optionCount;
		return HextechRuneSeenHistoryCodec.TryRead(payload, ref cursor, out seenOptionIds)
			&& finalOptionIds.All(seenOptionIds.Contains)
			&& HextechRuneWeightCodec.TryRead(payload, ref cursor, out _)
			&& HextechGeneratedRuneDataCodec.TryDecode(payload, cursor, finalOptionIds.Count, out _);
	}

	private static bool TryGetRuneIdForOrdinal(int ordinal, out ModelId id)
	{
		IReadOnlyList<ModelId> ids = PlayerRuneIdsByOrdinal.Value;
		if (ordinal < 0 || ordinal >= ids.Count)
		{
			id = null!;
			return false;
		}

		id = ids[ordinal];
		return true;
	}
}
