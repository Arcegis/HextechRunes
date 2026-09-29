namespace HextechRunes;

// 本次选择展示过的候选增量；包含被后续重随替换的候选，不受排除池耗尽时的清空影响。
internal static class HextechRuneSeenHistoryCodec
{
	internal const int Version = -12;

	internal static void Append(List<int> payload, IEnumerable<ModelId> seenIds)
	{
		ModelId[] ordered = seenIds.Distinct()
			.OrderBy(static id => id.Category, StringComparer.Ordinal)
			.ThenBy(static id => id.Entry, StringComparer.Ordinal)
			.ToArray();
		payload.Add(Version);
		HextechStableModelIdListCodec.Append(payload, ordered);
	}

	internal static bool TryRead(IReadOnlyList<int> payload, ref int cursor, out List<ModelId> seenIds)
	{
		seenIds = [];
		if (cursor < 0 || cursor >= payload.Count || payload[cursor] != Version
			|| !HextechStableModelIdListCodec.TryDecode(payload, cursor + 1, out seenIds, out int nextCursor)
			|| seenIds.Distinct().Count() != seenIds.Count)
		{
			return false;
		}

		cursor = nextCursor;
		return true;
	}
}
