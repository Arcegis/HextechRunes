namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	// 玩家候选只排除本局已见过的符文。敌我同名不再互相回避:敌方持有的海克斯照样可以出现在玩家候选里。
	private static HashSet<ModelId> CreateBaseExcludedIds(HextechMayhemModifier modifier, Player player)
	{
		return modifier.GetSeenPlayerRuneIds(player);
	}

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

	private static MonsterHexKind? FirstMonsterHexOrNull(IEnumerable<MonsterHexKind>? monsterHexes)
	{
		if (monsterHexes == null)
		{
			return null;
		}

		foreach (MonsterHexKind monsterHex in monsterHexes)
		{
			return monsterHex;
		}

		return null;
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
