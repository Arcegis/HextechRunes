namespace HextechRunes;

// 本次选择展示过的候选增量；包含被后续重随替换的候选，不受排除池耗尽时的清空影响。
internal static class HextechRuneSeenHistoryCodec
{
	internal const int Version = -13;

	internal static void Append(List<int> payload, IEnumerable<ModelId> seenIds)
	{
		ModelId[] ordered = seenIds.Distinct()
			.OrderBy(static id => id.Category, StringComparer.Ordinal)
			.ThenBy(static id => id.Entry, StringComparer.Ordinal)
			.ToArray();
		payload.Add(Version);
		payload.Add(ordered.Length);
		// 64 是单个稳定 ID 列表的上限；无限重随的完整历史按块编码，不能截断。
		foreach (ModelId[] chunk in ordered.Chunk(HextechStableModelIdListCodec.MaxCount))
		{
			HextechStableModelIdListCodec.Append(payload, chunk);
		}
	}

	internal static bool TryRead(IReadOnlyList<int> payload, ref int cursor, out List<ModelId> seenIds)
	{
		seenIds = [];
		if (cursor < 0 || cursor > payload.Count - 2 || payload[cursor] != Version)
		{
			return false;
		}

		int totalCount = payload[cursor + 1];
		int nextCursor = cursor + 2;
		// 每个 ID 至少占一个长度字段；先按实际载荷检查，不按不可信的总数分配内存。
		if (totalCount < 0 || totalCount > payload.Count - nextCursor)
		{
			return false;
		}

		List<ModelId> decoded = [];
		HashSet<ModelId> unique = [];
		while (decoded.Count < totalCount)
		{
			if (!HextechStableModelIdListCodec.TryDecode(payload, nextCursor, out List<ModelId> chunk, out int end)
				|| chunk.Count == 0 || chunk.Count > totalCount - decoded.Count)
			{
				return false;
			}
			foreach (ModelId id in chunk)
			{
				if (!unique.Add(id))
				{
					return false;
				}
			}
			decoded.AddRange(chunk);
			nextCursor = end;
		}

		seenIds = decoded;
		cursor = nextCursor;
		return true;
	}
}
