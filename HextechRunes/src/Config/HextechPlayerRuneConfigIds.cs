using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static class HextechPlayerRuneConfigIds
{
	public static HashSet<string> Normalize(IEnumerable<string>? ids)
	{
		HashSet<string> configurableIds = HextechCatalog.GetConfigurablePlayerRuneIds()
			.Select(static id => id.Entry)
			.ToHashSet(StringComparer.Ordinal);
		return (ids ?? [])
			.Where(static id => !string.IsNullOrWhiteSpace(id))
			.Select(static id => id.Trim())
			.Distinct(StringComparer.Ordinal)
			.Where(configurableIds.Contains)
			.OrderBy(static id => id, StringComparer.Ordinal)
			.ToHashSet(StringComparer.Ordinal);
	}

	public static HashSet<string> FromTypes(IEnumerable<Type> runeTypes)
	{
		return Normalize(runeTypes
			.Select(ModelDb.GetId)
			.Select(static id => id.Entry));
	}
}
