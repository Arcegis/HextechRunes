namespace HextechRunes;

internal static class HextechPlayerRuneConfigIds
{
	public static HashSet<string> Normalize(IEnumerable<string>? ids, bool preserveUnknownIds = true)
	{
		if (preserveUnknownIds)
		{
			return HextechRuneConfiguration.NormalizeConfigStringIds(ids);
		}

		// 可配置集合在注册表校验失败时会抛异常;加载配置走 preserveUnknownIds=true,不读它,
		// 以免一处注册冲突让整份配置回落为默认。
		HashSet<string> configurableIds = HextechCatalog.GetConfigurablePlayerRuneIds()
			.Select(static id => id.Entry)
			.ToHashSet(StringComparer.Ordinal);
		return HextechRuneConfiguration.NormalizeStringIds(ids, configurableIds);
	}

	public static HashSet<string> FromTypes(IEnumerable<Type> runeTypes)
	{
		return Normalize(runeTypes
			.Select(ModelDb.GetId)
			.Select(static id => id.Entry),
			preserveUnknownIds: false);
	}
}
