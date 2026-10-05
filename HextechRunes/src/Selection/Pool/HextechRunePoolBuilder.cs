namespace HextechRunes;

internal static class HextechRunePoolBuilder
{
	private const int RuneTagBiasBaseWeight = 100;
	private const int RuneTagBiasNormalBonusPerMatch = 25;
	private const int RuneTagBiasEndlessBonusPerMatch = 20;
	private const int RuneTagBiasMaxBonus = 50;
	private const int RuneTagBiasEndlessHistoryWindow = 3;
	// 每次海克斯选择给出的候选数(三选一)。
	private const int MaxRuneOptionCount = 3;

	/// <summary>给候选池里已按权重算好的一次抽取返回 roll(0 ≤ roll &lt; totalWeight)。pickIndex 为本次是第几个候选。</summary>
	private delegate int WeightedRoll(IReadOnlyList<RelicModel> slotCandidates, IReadOnlyList<int> weights, int totalWeight, int pickIndex);

	public static List<RelicModel> BuildSelectableRunePool(Player player, HextechRarityTier rarity, RunState runState, IReadOnlySet<ModelId>? excludedIds = null)
	{
		HashSet<ModelId> ownedIds = player.Relics
			.Where(HextechCatalog.IsHextechRelic)
			.Select(static relic => relic.CanonicalId())
			.ToHashSet();
		HashSet<ModelId> blockedOwnedIds = ownedIds.ToHashSet();
		blockedOwnedIds.UnionWith(HextechCatalog.GetMutuallyExclusivePlayerRuneIds(ownedIds));
		List<RelicModel> pool = HextechCatalog.GetConfigurablePlayerRuneTypesForRarity(rarity)
			.Where(HextechRuntimeRuneCompatibility.IsPlayerRuneAvailableForCurrentRuntime)
			.Where(type => HextechCatalog.IsPlayerRuneAllowedInAct(type, runState.CurrentActIndex))
			.Select(static type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
			.Where(relic => HextechCatalog.IsAvailableForPlayer(relic, player)
				&& !blockedOwnedIds.Contains(relic.CanonicalId())
				&& (excludedIds == null || !excludedIds.Contains(relic.CanonicalId())))
			.ToList();

		return ApplyPlayerRuneConfiguration(pool, runState);
	}

	public static List<RelicModel> BuildSelectableRunesForRarity(
		Player player,
		HextechRarityTier rarity,
		RunState runState,
		IReadOnlySet<ModelId>? excludedIds = null,
		bool useEndlessTagWindow = false)
	{
		List<RelicModel> pool = BuildPoolFallingBackFromSeen(player, rarity, runState, excludedIds, out _);
		Dictionary<string, int> tagCounts = BuildOwnedRuneTagCounts(player, useEndlessTagWindow);
		// 单机:候选按池原顺序,每次抽取推进一次原版 Niche 随机流。
		HextechWeightedRuneOptions picked = PickWeightedDistinct(
			player,
			pool,
			Math.Min(MaxRuneOptionCount, pool.Count),
			tagCounts,
			useEndlessTagWindow,
			(_, _, totalWeight, _) => runState.Rng.Niche.NextInt(totalWeight));
		List<RelicModel> options = picked.Select(relic => CreateSelectableRuneOption(player, relic)).ToList();
		return new HextechWeightedRuneOptions(HextechRuneGeneration.Transform(player, rarity, runState, -1, options), picked.CharacterWeightPercent);
	}

	public static List<RelicModel> BuildStableSelectableRunesForRarity(
		Player player,
		HextechRarityTier rarity,
		RunState runState,
		int selectionStageIndex,
		IReadOnlySet<ModelId>? excludedIds = null,
		bool useEndlessTagWindow = false)
	{
		// 回退到见过没选的池时,稳定随机的 salt 也一致地忽略「已见」,使重连/重开重建时能复现同一组选项。
		List<RelicModel> pool = BuildPoolFallingBackFromSeen(player, rarity, runState, excludedIds, out IReadOnlySet<ModelId>? effectiveExcludedIds);
		Dictionary<string, int> tagCounts = BuildOwnedRuneTagCounts(player, useEndlessTagWindow);
		string?[] saltParts =
		[
			"rune-selection-options",
			selectionStageIndex.ToString(),
			HextechStableRandom.PlayerKey(player),
			((int)rarity).ToString(),
			effectiveExcludedIds == null ? "" : string.Join(",", effectiveExcludedIds.Select(static id => id.Entry).OrderBy(static entry => entry, StringComparer.Ordinal))
		];
		// 联机:候选按 ModelId 排序,每次抽取用含候选与权重的稳定哈希,各端独立算出同一组选项。
		HextechWeightedRuneOptions picked = PickWeightedDistinct(
			player,
			pool.OrderBy(static relic => relic.CanonicalId().Entry, StringComparer.Ordinal).ToList(),
			Math.Min(MaxRuneOptionCount, pool.Count),
			tagCounts,
			useEndlessTagWindow,
			(slotCandidates, weights, totalWeight, pickIndex) => HextechStableRandom.Index(
				runState,
				totalWeight,
				HextechStableRandom.AppendSalt(
					saltParts,
					"pick",
					pickIndex.ToString(),
					"player",
					HextechStableRandom.PlayerKey(player),
					"pool",
					BuildWeightedPoolKey(slotCandidates, weights))));
		return new HextechWeightedRuneOptions(HextechRuneGeneration.Transform(player, rarity, runState, selectionStageIndex,
			picked.Select(relic => CreateSelectableRuneOption(player, relic)).ToList()), picked.CharacterWeightPercent);
	}

	/// <summary>
	/// excludedIds 是本局「已展示过(seen)」的符文。长局/无尽里某稀有度几乎都被展示过时,这层排除会清空池:
	/// 此时放宽到忽略「已见」(仍排除已拥有/互斥/禁用),未见的抽完才回到见过没选的。真正一个都不剩时由调用方
	/// 显示"继续"界面。effectiveExcludedIds 是实际生效的排除集合,回退时为 null。不消耗随机数。
	/// </summary>
	private static List<RelicModel> BuildPoolFallingBackFromSeen(
		Player player,
		HextechRarityTier rarity,
		RunState runState,
		IReadOnlySet<ModelId>? excludedIds,
		out IReadOnlySet<ModelId>? effectiveExcludedIds)
	{
		effectiveExcludedIds = excludedIds;
		List<RelicModel> pool = BuildSelectableRunePool(player, rarity, runState, excludedIds);
		if (pool.Count > 0 || excludedIds is not { Count: > 0 })
		{
			return pool;
		}

		List<RelicModel> fallbackPool = BuildSelectableRunePool(player, rarity, runState, null);
		if (fallbackPool.Count == 0)
		{
			return pool;
		}

		HextechLog.Warn("Mayhem", $"{rarity} rune option pool exhausted by seen-history; falling back to the full pool (ignoring seen) so the selection is not emptied.");
		effectiveExcludedIds = null;
		return fallbackPool;
	}

	public static Dictionary<string, int> BuildOwnedRuneTagCounts(Player player, bool useEndlessTagWindow)
	{
		List<RelicModel> ownedRunes = player.Relics
			.Where(HextechCatalog.IsHextechRelic)
			.ToList();
		int startIndex = useEndlessTagWindow
			? Math.Max(0, ownedRunes.Count - RuneTagBiasEndlessHistoryWindow)
			: 0;

		Dictionary<string, int> counts = new(StringComparer.Ordinal);
		for (int i = startIndex; i < ownedRunes.Count; i++)
		{
			string tagKey = HextechCatalog.GetPlayerRuneTagKey(ownedRunes[i]);
			counts[tagKey] = counts.TryGetValue(tagKey, out int count) ? count + 1 : 1;
		}

		return counts;
	}

	public static List<int> BuildRuneTagWeights(
		IReadOnlyList<RelicModel> pool,
		IReadOnlyDictionary<string, int> tagCounts,
		bool useEndlessTagWindow,
		out int totalWeight)
	{
		List<int> weights = new(pool.Count);
		totalWeight = 0;
		foreach (RelicModel relic in pool)
		{
			int weight = GetRuneTagWeight(relic, tagCounts, useEndlessTagWindow);
			weights.Add(weight);
			totalWeight += weight;
		}

		return weights;
	}

	internal static int GetSavedCharacterWeight(Player player)
	{
		return HextechMayhemModifier.FindIn(player.RunState)
			?.GetCharacterRuneWeight(player.NetId) ?? HextechWeightedRuneOptions.InitialCharacterWeightPercent;
	}

	internal static int AdvanceCharacterWeight(Player player, int weight, RelicModel drawn)
	{
		PlayerRuneCharacterPool? character = GetRuneCharacterPool(player);
		return character.HasValue
			? HextechWeightedRuneOptions.Advance(weight, IsRuneForCharacter(drawn, character)) : weight;
	}

	internal static List<int> BuildSelectionWeights(IReadOnlyList<RelicModel> pool,
		IReadOnlyDictionary<string, int> tagCounts, bool useEndlessTagWindow,
		PlayerRuneCharacterPool? character, int characterWeightPercent, out int totalWeight)
	{
		List<int> weights = BuildRuneTagWeights(pool, tagCounts, useEndlessTagWindow, out _);
		totalWeight = 0;
		for (int i = 0; i < weights.Count; i++)
		{
			// 同时放大通用权重，保留 125 × 150% 等组合的小数精度。
			weights[i] = checked(weights[i] * (IsRuneForCharacter(pool[i], character) ? characterWeightPercent : 100));
			totalWeight = checked(totalWeight + weights[i]);
		}
		// 配置可能只留下专属池；权重归零时仍须给出合法选项，不能让选择流程卡死。
		return totalWeight > 0 ? weights : BuildRuneTagWeights(pool, tagCounts, useEndlessTagWindow, out totalWeight);
	}

	public static int SelectWeightedIndex(IReadOnlyList<int> weights, int roll)
	{
		for (int i = 0; i < weights.Count; i++)
		{
			if (roll < weights[i])
			{
				return i;
			}

			roll -= weights[i];
		}

		return Math.Max(0, weights.Count - 1);
	}

	public static RelicModel CreateSelectableRuneOption(Player player, RelicModel relic)
	{
		RelicModel option = relic.ToMutable();
		if (option is HextechRelicBase hextechOption)
		{
			hextechOption.RefreshDescriptionForPlayer(player);
		}

		return option;
	}

	// 按类名后缀识别“升级类”符文(每次三选一最多出现一个):它们没有共同基类——多数继承
	// CardUpgradeRuneBase,也有 AutoPlayForms/SelfUpgradeOnPlay 基类以及直接继承 HextechRelicBase 的
	// UpgradeRune/StrikeUpgradeRune/DefendUpgradeRune,外部模组也沿用这一命名约定。换成类型判定会改变结果集合。
	internal static bool IsUpgradeRune(RelicModel relic)
	{
		Type type = (relic.CanonicalInstance ?? relic).GetType();
		return type.Name.EndsWith("UpgradeRune", StringComparison.Ordinal);
	}

	internal static List<RelicModel> ConstrainCandidates(
		IEnumerable<RelicModel> candidates,
		bool upgradeAlreadySelected)
	{
		return candidates
			.Where(relic => !upgradeAlreadySelected || !IsUpgradeRune(relic))
			.ToList();
	}

	// 以首个候选的登记稀有度为准;不是可配置玩家符文(或列表为空)时按 Gold 处理,与原先逐个比对可配置列表的结果相同。
	public static HextechRarityTier GetRarityForOptions(IReadOnlyList<RelicModel> relics)
	{
		if (relics.Count == 0)
		{
			return HextechRarityTier.Gold;
		}

		Type runeType = (relics[0].CanonicalInstance ?? relics[0]).GetType();
		PlayerRuneMetadataCatalog metadata = HextechContentRegistry.PlayerRuneMetadata;
		return metadata.IsConfigurable(runeType) && metadata.TryGetRarity(runeType, out HextechRarityTier rarity)
			? rarity
			: HextechRarityTier.Gold;
	}

	private static List<RelicModel> ApplyPlayerRuneConfiguration(List<RelicModel> pool, RunState runState)
	{
		IReadOnlySet<string> disabledIds = GetEffectiveDisabledPlayerRuneIds(runState);
		if (disabledIds.Count == 0)
		{
			return pool;
		}

		return FilterDisabledPlayerRunes(pool, disabledIds);
	}

	internal static List<RelicModel> FilterDisabledPlayerRunes(
		IEnumerable<RelicModel> pool,
		IReadOnlySet<string> disabledIds)
	{
		return pool
			.Where(relic =>
			{
				ModelId id = relic.CanonicalId();
				return !disabledIds.Contains(id.Entry);
			})
			.ToList();
	}

	internal static IReadOnlySet<string> GetEffectiveDisabledPlayerRuneIds(RunState runState)
	{
		if (HextechMayhemModifier.FindIn(runState) is HextechMayhemModifier modifier)
		{
			return modifier.PlayerRuneConfigDisabledIds;
		}

		// 联机客户端没有本局快照时不能用本地菜单值,按空集处理。
		return HextechPlayerContextHelper.IsClientRun(fallbackWhenUnavailable: true)
			? new HashSet<string>(StringComparer.Ordinal)
			: HextechRuneConfiguration.GetDisabledPlayerRuneIds();
	}

	internal static IReadOnlyList<HextechRarityTier> GetEnabledPlayerRuneRarities(RunState runState)
	{
		IReadOnlySet<string> disabledIds = GetEffectiveDisabledPlayerRuneIds(runState);
		return GetEnabledPlayerRuneRaritiesForDisabledIds(disabledIds);
	}

	internal static IReadOnlyList<HextechRarityTier> GetEnabledPlayerRuneRaritiesForDisabledIds(IReadOnlySet<string> disabledIds)
	{
		HextechRarityTier[] enabledRarities = Enum.GetValues<HextechRarityTier>()
			.Where(rarity => HasEnabledConfigurablePlayerRuneForRarity(rarity, disabledIds))
			.ToArray();

		return enabledRarities.Length > 0
			? enabledRarities
			: Enum.GetValues<HextechRarityTier>();
	}

	private static bool HasEnabledConfigurablePlayerRuneForRarity(HextechRarityTier rarity, IReadOnlySet<string> disabledIds)
	{
		return HextechCatalog.GetConfigurablePlayerRuneTypesForRarity(rarity)
			.Where(HextechRuntimeRuneCompatibility.IsPlayerRuneAvailableForCurrentRuntime)
			.Any(type => !disabledIds.Contains(ModelDb.GetId(type).Entry));
	}

	/// <summary>
	/// 按标签/角色权重从 pool 里不放回地抽 count 个候选(同一次选择里最多一个升级类符文)。
	/// 单机与联机只差 roll 的来源;两者的抽取次数与顺序与拆分前一致。pool 会被消耗。
	/// </summary>
	private static HextechWeightedRuneOptions PickWeightedDistinct(
		Player player,
		List<RelicModel> pool,
		int count,
		IReadOnlyDictionary<string, int> tagCounts,
		bool useEndlessTagWindow,
		WeightedRoll roll)
	{
		List<RelicModel> selected = new(Math.Min(Math.Max(0, count), pool.Count));
		int characterWeight = GetSavedCharacterWeight(player);
		for (int i = 0; i < count && pool.Count > 0; i++)
		{
			bool upgradeAlreadySelected = selected.Any(IsUpgradeRune);
			List<RelicModel> slotCandidates = ConstrainCandidates(
				pool,
				upgradeAlreadySelected);
			if (slotCandidates.Count == 0)
			{
				break;
			}

			List<int> weights = BuildSelectionWeights(slotCandidates, tagCounts, useEndlessTagWindow, GetRuneCharacterPool(player), characterWeight, out int totalWeight);
			int index = SelectWeightedIndex(weights, roll(slotCandidates, weights, totalWeight, i));
			RelicModel chosen = slotCandidates[index];
			selected.Add(chosen);
			characterWeight = AdvanceCharacterWeight(player, characterWeight, chosen);
			ModelId chosenId = chosen.CanonicalId();
			pool.RemoveAll(relic => relic.CanonicalId() == chosenId);
		}

		return new HextechWeightedRuneOptions(selected, characterWeight);
	}

	internal static PlayerRuneCharacterPool? GetRuneCharacterPool(Player player)
	{
		return HextechPlayerContextHelper.TryGetRuneCharacterPool(player, out PlayerRuneCharacterPool characterPool)
			? characterPool
			: null;
	}

	internal static bool IsRuneForCharacter(RelicModel relic, PlayerRuneCharacterPool? characterPool)
	{
		return characterPool.HasValue
			&& HextechContentRegistry.PlayerRuneMetadata.TryGetRegistration(
				(relic.CanonicalInstance ?? relic).GetType(),
				out PlayerRuneRegistration registration)
			&& registration.CharacterPool == characterPool;
	}

	private static int GetRuneTagWeight(RelicModel relic, IReadOnlyDictionary<string, int> tagCounts, bool useEndlessTagWindow)
	{
		string tagKey = HextechCatalog.GetPlayerRuneTagKey(relic);
		int weight = RuneTagBiasBaseWeight;
		if (tagCounts.TryGetValue(tagKey, out int matchingCount) && matchingCount > 0)
		{
			int bonusPerMatch = useEndlessTagWindow
				? RuneTagBiasEndlessBonusPerMatch
				: RuneTagBiasNormalBonusPerMatch;
			weight += Math.Min(RuneTagBiasMaxBonus, matchingCount * bonusPerMatch);
		}

		return weight;
	}

	private static string BuildWeightedPoolKey(IReadOnlyList<RelicModel> pool, IReadOnlyList<int> weights)
	{
		return string.Join(",", pool.Select((relic, index) => $"{relic.CanonicalId().Entry}:{weights[index]}"));
	}
}
