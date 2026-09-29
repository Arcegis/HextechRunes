namespace HextechRunes;

internal static class HextechPlayerRuneConfigIds
{
	public static HashSet<string> Normalize(IEnumerable<string>? ids, bool preserveUnknownIds = true)
	{
		IEnumerable<string> normalized = (ids ?? [])
			.Where(static id => !string.IsNullOrWhiteSpace(id))
			.Select(static id => id.Trim());

		if (!preserveUnknownIds)
		{
			// 只在需要过滤时才构建可配置集合:它要做注册表唯一性校验与分组,可能抛异常;
			// 加载配置走 preserveUnknownIds=true,不应因此让整份配置被兜底回落为默认。
			HashSet<string> configurableIds = HextechCatalog.GetConfigurablePlayerRuneIds()
				.Select(static id => id.Entry)
				.ToHashSet(StringComparer.Ordinal);
			normalized = normalized.Where(configurableIds.Contains);
		}

		// ToHashSet 自己去重;排序让只插入的 HashSet 按稳定顺序枚举(落盘与日志)。
		return normalized
			.OrderBy(static id => id, StringComparer.Ordinal)
			.ToHashSet(StringComparer.Ordinal);
	}

	public static HashSet<string> FromTypes(IEnumerable<Type> runeTypes)
	{
		return Normalize(runeTypes
			.Select(ModelDb.GetId)
			.Select(static id => id.Entry),
			preserveUnknownIds: false);
	}
}
