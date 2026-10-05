namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	private static HashSet<ModelId> CreateSeenOptionIds(IEnumerable<RelicModel> options, IEnumerable<ModelId>? alreadySeenIds = null)
	{
		HashSet<ModelId> seenOptionIds = options
			.Select(static relic => relic.CanonicalId())
			.ToHashSet();
		if (alreadySeenIds != null)
		{
			seenOptionIds.UnionWith(alreadySeenIds);
		}

		return seenOptionIds;
	}

	// 敌方重掷只避开同一界面上其他敌方槽位的海克斯,不看玩家候选。
	private static HashSet<ModelId> CreateEnemyHexRerollExcludedIds(IReadOnlyList<MonsterHexKind?> currentMonsterHexes, int rerollSlotIndex)
	{
		HashSet<ModelId> excludedIds = [];
		for (int i = 0; i < currentMonsterHexes.Count; i++)
		{
			if (i != rerollSlotIndex && currentMonsterHexes[i] is MonsterHexKind hex)
			{
				excludedIds.Add(GetMonsterHexIconRelicId(hex));
			}
		}

		return excludedIds;
	}

	public static void RemoveRunesFromGrabBags(Player player)
	{
		foreach (RelicModel relic in HextechCatalog.GetCanonicalRunes())
		{
			player.RelicGrabBag.Remove(relic);
			player.RunState.SharedRelicGrabBag.Remove(relic);
		}
	}

	private static bool IsCurrentRun(RunState runState)
	{
		return ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), runState);
	}
}
