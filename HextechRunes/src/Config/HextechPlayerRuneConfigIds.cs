namespace HextechRunes;

internal static class HextechPlayerRuneConfigIds
{
	public static HashSet<string> Normalize(IEnumerable<string>? ids, bool preserveUnknownIds = true)
	{
		HashSet<string> configurableIds = HextechCatalog.GetConfigurablePlayerRuneIds()
			.Select(static id => id.Entry)
			.ToHashSet(StringComparer.Ordinal);
		IEnumerable<string> normalized = (ids ?? [])
			.Where(static id => !string.IsNullOrWhiteSpace(id))
			.Select(static id => id.Trim())
			.Distinct(StringComparer.Ordinal);

		if (!preserveUnknownIds)
		{
			normalized = normalized.Where(configurableIds.Contains);
		}

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
