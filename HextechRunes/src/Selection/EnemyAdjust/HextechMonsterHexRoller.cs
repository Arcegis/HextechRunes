using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static class HextechMonsterHexRoller
{
	public static IReadOnlyList<MonsterHexKind> BuildActPool(
		HextechRarityTier rarity,
		IEnumerable<MonsterHexKind>? knownHexes,
		IEnumerable<MonsterHexKind>? extraExcludedHexes = null)
	{
		IReadOnlyList<MonsterHexKind> rarityPool = MonsterHexCatalog.GetMonsterHexesForRarity(rarity);
		HashSet<MonsterHexKind> excluded = ToSet(knownHexes);
		if (extraExcludedHexes != null)
		{
			excluded.UnionWith(extraExcludedHexes);
		}

		List<MonsterHexKind> pool = rarityPool
			.Where(kind => !excluded.Contains(kind))
			.ToList();
		return pool.Count > 0 ? pool : rarityPool.ToArray();
	}

	public static IReadOnlyList<MonsterHexKind> ResolveNewMonsterHexes(
		int newEnemyHexCount,
		IEnumerable<MonsterHexKind> previousHexes,
		MonsterHexKind? primaryMonsterHex,
		Func<IReadOnlySet<MonsterHexKind>, int, MonsterHexKind?> chooseExtraHex)
	{
		if (newEnemyHexCount <= 0)
		{
			return [];
		}

		List<MonsterHexKind> resolvedNewHexes = [];
		HashSet<MonsterHexKind> seen = ToSet(previousHexes);

		if (primaryMonsterHex.HasValue && seen.Add(primaryMonsterHex.Value))
		{
			resolvedNewHexes.Add(primaryMonsterHex.Value);
		}

		for (int ordinal = 1; resolvedNewHexes.Count < newEnemyHexCount; ordinal++)
		{
			MonsterHexKind? extraHex = chooseExtraHex(seen, ordinal);
			if (!extraHex.HasValue || !seen.Add(extraHex.Value))
			{
				break;
			}

			resolvedNewHexes.Add(extraHex.Value);
		}

		return resolvedNewHexes;
	}

	public static IReadOnlyList<MonsterHexKind> CombineActiveHexes(
		IEnumerable<MonsterHexKind> previousHexes,
		IEnumerable<MonsterHexKind> newHexes)
	{
		List<MonsterHexKind> combined = [];
		HashSet<MonsterHexKind> seen = [];
		AddUnique(combined, seen, previousHexes);
		AddUnique(combined, seen, newHexes);
		return combined;
	}

	public static IReadOnlyList<MonsterHexKind> BuildRerollPool(
		HextechRarityTier rarity,
		IEnumerable<MonsterHexKind> knownHexes,
		MonsterHexKind? currentHex,
		IReadOnlySet<ModelId> excludedIconRelicIds,
		Func<MonsterHexKind, ModelId> getIconRelicId)
	{
		IReadOnlyList<MonsterHexKind> rarityPool = MonsterHexCatalog.GetMonsterHexesForRarity(rarity);
		HashSet<MonsterHexKind> alreadyChosen = knownHexes
			.Where(kind => kind != currentHex)
			.ToHashSet();

		List<MonsterHexKind> pool = rarityPool
			.Where(kind => kind != currentHex)
			.Where(kind => !alreadyChosen.Contains(kind))
			.Where(kind => !excludedIconRelicIds.Contains(getIconRelicId(kind)))
			.ToList();
		if (pool.Count > 0)
		{
			return pool;
		}

		pool = rarityPool
			.Where(kind => kind != currentHex)
			.Where(kind => !alreadyChosen.Contains(kind))
			.ToList();
		if (pool.Count > 0)
		{
			return pool;
		}

		pool = rarityPool
			.Where(kind => kind != currentHex)
			.ToList();
		return pool.Count > 0 ? pool : rarityPool.ToArray();
	}

	private static HashSet<MonsterHexKind> ToSet(IEnumerable<MonsterHexKind>? hexes)
	{
		return hexes?.ToHashSet() ?? [];
	}

	private static void AddUnique(
		List<MonsterHexKind> target,
		HashSet<MonsterHexKind> seen,
		IEnumerable<MonsterHexKind> source)
	{
		foreach (MonsterHexKind hex in source)
		{
			if (seen.Add(hex))
			{
				target.Add(hex);
			}
		}
	}
}
