using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static class HextechMonsterHexRoller
{
	// 联机下禁用的敌方 hex:实时(墙钟)回血「自然即是治愈」在 lockstep 多人下无法确定性同步,
	// 直接从 roll 池排除,让联机时敌方抽不到。单机不受影响。玩家侧对应排除见
	// NatureIsHealingRune.IsAvailableForPlayer。只排除 roll 池,不动注册/枚举/效果,保存档兼容。
	private static readonly IReadOnlySet<MonsterHexKind> MultiplayerDisabledHexes =
		new HashSet<MonsterHexKind> { MonsterHexKind.NatureIsHealing };

	public static IReadOnlyList<MonsterHexKind> BuildActPool(
		HextechRarityTier rarity,
		IEnumerable<MonsterHexKind>? knownHexes,
		IEnumerable<MonsterHexKind>? extraExcludedHexes = null,
		IReadOnlySet<string>? disabledMonsterHexIds = null)
	{
		IReadOnlyList<MonsterHexKind> rarityPool = ApplyConfig(FilterMultiplayerDisabled(MonsterHexCatalog.GetMonsterHexesForRarity(rarity)), disabledMonsterHexIds);
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
		Func<MonsterHexKind, ModelId> getIconRelicId,
		IReadOnlySet<string>? disabledMonsterHexIds = null)
	{
		IReadOnlyList<MonsterHexKind> rarityPool = ApplyConfig(FilterMultiplayerDisabled(MonsterHexCatalog.GetMonsterHexesForRarity(rarity)), disabledMonsterHexIds);
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

	// 联机时从 rarity 池移除「联机禁用」的 hex。IsNetworkMultiplayerRun() 两端一致 →
	// 各 peer 得到同一池 → roll 确定性,不引入分叉。为空兜底返回原池(防御性,当前禁用集不会清空 Gold 池)。
	private static IReadOnlyList<MonsterHexKind> FilterMultiplayerDisabled(IReadOnlyList<MonsterHexKind> rarityPool)
	{
		if (MultiplayerDisabledHexes.Count == 0 || !HextechPlayerContextHelper.IsNetworkMultiplayerRun())
		{
			return rarityPool;
		}

		List<MonsterHexKind> filtered = rarityPool
			.Where(kind => !MultiplayerDisabledHexes.Contains(kind))
			.ToList();
		return filtered.Count > 0 ? filtered : rarityPool;
	}

	private static IReadOnlyList<MonsterHexKind> ApplyConfig(
		IReadOnlyList<MonsterHexKind> rarityPool,
		IReadOnlySet<string>? disabledMonsterHexIds)
	{
		if (disabledMonsterHexIds == null || disabledMonsterHexIds.Count == 0)
		{
			return rarityPool;
		}

		List<MonsterHexKind> configuredPool = rarityPool
			.Where(kind => !disabledMonsterHexIds.Contains(kind.ToString()))
			.ToList();
		return configuredPool.Count > 0 ? configuredPool : [];
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
