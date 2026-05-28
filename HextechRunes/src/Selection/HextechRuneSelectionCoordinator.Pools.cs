using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	private static List<RelicModel> BuildSelectableRunePool(Player player, HextechRarityTier rarity, RunState runState, IReadOnlySet<ModelId>? excludedIds = null)
	{
		HashSet<ModelId> ownedIds = player.Relics
			.Where(HextechCatalog.IsHextechRelic)
			.Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id)
			.ToHashSet();
		HashSet<ModelId> blockedOwnedIds = ownedIds.ToHashSet();
		blockedOwnedIds.UnionWith(HextechCatalog.GetMutuallyExclusivePlayerRuneIds(ownedIds));

		List<RelicModel> pool = HextechCatalog.GetPlayerRuneTypesForRarity(rarity)
			.Where(type => HextechCatalog.IsPlayerRuneAllowedInAct(type, runState.CurrentActIndex))
			.Select(static type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
			.Where(relic => HextechCatalog.IsAvailableForPlayer(relic, player)
				&& !blockedOwnedIds.Contains(relic.CanonicalInstance?.Id ?? relic.Id)
				&& (excludedIds == null || !excludedIds.Contains(relic.CanonicalInstance?.Id ?? relic.Id)))
			.ToList();

		return pool;
	}

	private static List<RelicModel> BuildSelectableRunesForRarity(
		Player player,
		HextechRarityTier rarity,
		RunState runState,
		IReadOnlySet<ModelId>? excludedIds = null,
		bool useEndlessTagWindow = false)
	{
		List<RelicModel> pool = BuildSelectableRunePool(player, rarity, runState, excludedIds);
		Dictionary<string, int> tagCounts = BuildOwnedRuneTagCounts(player, useEndlessTagWindow);

		List<RelicModel> options = [];
		int picks = Math.Min(3, pool.Count);
		for (int i = 0; i < picks; i++)
		{
			List<int> weights = BuildRuneTagWeights(pool, tagCounts, useEndlessTagWindow, out int totalWeight);
			int index = SelectWeightedIndex(weights, runState.Rng.Niche.NextInt(totalWeight));
			options.Add(CreateSelectableRuneOption(player, pool[index]));
			pool.RemoveAt(index);
		}

		return options;
	}

	private static List<RelicModel> BuildStableSelectableRunesForRarity(
		Player player,
		HextechRarityTier rarity,
		RunState runState,
		IReadOnlySet<ModelId>? excludedIds = null,
		bool useEndlessTagWindow = false)
	{
		List<RelicModel> pool = BuildSelectableRunePool(player, rarity, runState, excludedIds);
		Dictionary<string, int> tagCounts = BuildOwnedRuneTagCounts(player, useEndlessTagWindow);
		int picks = Math.Min(3, pool.Count);
		return PickStableWeightedDistinct(
			player,
			pool,
			picks,
			runState,
			tagCounts,
			useEndlessTagWindow,
			"rune-selection-options",
			runState.CurrentActIndex.ToString(),
			HextechStableRandom.PlayerKey(player),
			((int)rarity).ToString(),
			excludedIds == null ? "" : string.Join(",", excludedIds.Select(static id => id.Entry).OrderBy(static entry => entry, StringComparer.Ordinal)))
			.Select(relic => CreateSelectableRuneOption(player, relic))
			.ToList();
	}

	private static Dictionary<string, int> BuildOwnedRuneTagCounts(Player player, bool useEndlessTagWindow)
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

	private static List<RelicModel> PickStableWeightedDistinct(
		Player player,
		IEnumerable<RelicModel> candidates,
		int count,
		RunState runState,
		IReadOnlyDictionary<string, int> tagCounts,
		bool useEndlessTagWindow,
		params string?[] saltParts)
	{
		List<RelicModel> pool = candidates
			.OrderBy(static relic => (relic.CanonicalInstance?.Id ?? relic.Id).Entry, StringComparer.Ordinal)
			.ToList();
		List<RelicModel> selected = new(Math.Min(Math.Max(0, count), pool.Count));
		for (int i = 0; i < count && pool.Count > 0; i++)
		{
			List<int> weights = BuildRuneTagWeights(pool, tagCounts, useEndlessTagWindow, out int totalWeight);
			string poolKey = BuildWeightedPoolKey(pool, weights);
			int roll = HextechStableRandom.Index(
				runState,
				totalWeight,
				AppendSelectionSalt(
					saltParts,
					"pick",
					i.ToString(),
					"player",
					HextechStableRandom.PlayerKey(player),
					"pool",
					poolKey));
			int index = SelectWeightedIndex(weights, roll);
			selected.Add(pool[index]);
			pool.RemoveAt(index);
		}

		return selected;
	}

	private static List<int> BuildRuneTagWeights(
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

	private static int GetRuneTagWeight(RelicModel relic, IReadOnlyDictionary<string, int> tagCounts, bool useEndlessTagWindow)
	{
		string tagKey = HextechCatalog.GetPlayerRuneTagKey(relic);
		if (!tagCounts.TryGetValue(tagKey, out int matchingCount) || matchingCount <= 0)
		{
			return RuneTagBiasBaseWeight;
		}

		int bonusPerMatch = useEndlessTagWindow
			? RuneTagBiasEndlessBonusPerMatch
			: RuneTagBiasNormalBonusPerMatch;
		int bonus = Math.Min(RuneTagBiasMaxBonus, matchingCount * bonusPerMatch);
		return RuneTagBiasBaseWeight + bonus;
	}

	private static int SelectWeightedIndex(IReadOnlyList<int> weights, int roll)
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

	private static string BuildWeightedPoolKey(IReadOnlyList<RelicModel> pool, IReadOnlyList<int> weights)
	{
		return string.Join(",", pool.Select((relic, index) => $"{(relic.CanonicalInstance?.Id ?? relic.Id).Entry}:{weights[index]}"));
	}

	private static string?[] AppendSelectionSalt(string?[] saltParts, params string?[] extra)
	{
		string?[] result = new string?[saltParts.Length + extra.Length];
		Array.Copy(saltParts, result, saltParts.Length);
		Array.Copy(extra, 0, result, saltParts.Length, extra.Length);
		return result;
	}

	private static RelicModel CreateSelectableRuneOption(Player player, RelicModel relic)
	{
		RelicModel option = relic.ToMutable();
		RefreshPlayerContextualRuneDescription(player, option);
		return option;
	}

	private static void RefreshPlayerContextualRuneDescription(Player player, RelicModel relic)
	{
		if (relic is FlyingKickRune flyingKickRune)
		{
			flyingKickRune.RefreshExecutePercent(player.Creature.MaxHp);
		}
	}

	private static HashSet<ModelId> CreateBaseExcludedIds(HextechMayhemModifier modifier, Player player, RelicModel? monsterHexRelic)
	{
		HashSet<ModelId> excludedIds = modifier.GetSeenPlayerRuneIds(player);
		if (monsterHexRelic != null)
		{
			excludedIds.Add(monsterHexRelic.CanonicalInstance?.Id ?? monsterHexRelic.Id);
		}

		return excludedIds;
	}

	private static HashSet<ModelId> CreateSeenOptionIds(IEnumerable<RelicModel> options, RelicModel? monsterHexRelic, IEnumerable<ModelId>? alreadySeenIds = null)
	{
		HashSet<ModelId> seenOptionIds = options
			.Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id)
			.ToHashSet();
		if (alreadySeenIds != null)
		{
			seenOptionIds.UnionWith(alreadySeenIds);
		}

		if (monsterHexRelic != null)
		{
			seenOptionIds.Add(monsterHexRelic.CanonicalInstance?.Id ?? monsterHexRelic.Id);
		}

		return seenOptionIds;
	}

	private static RelicModel? CreateMonsterHexRelic(MonsterHexKind? monsterHex)
	{
		return monsterHex.HasValue
			? MonsterHexCatalog.GetIconRelicForMonsterHex(monsterHex.Value).ToMutable()
			: null;
	}

	private static HashSet<ModelId> CreateEnemyHexRerollExcludedIds(IEnumerable<RelicModel> options)
	{
		return options
			.Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id)
			.ToHashSet();
	}

	private static void MarkRelicsSeen(IEnumerable<RelicModel> relics)
	{
		foreach (RelicModel relic in relics)
		{
			SaveManager.Instance.MarkRelicAsSeen(relic);
		}
	}

	private static HextechRarityTier GetRarityForOptions(IReadOnlyList<RelicModel> relics)
	{
		if (relics.Count == 0)
		{
			return HextechRarityTier.Gold;
		}

		ModelId id = relics[0].CanonicalInstance?.Id ?? relics[0].Id;
		if (HextechCatalog.GetPlayerRuneTypesForRarity(HextechRarityTier.Silver).Any(type => ModelDb.GetId(type) == id))
		{
			return HextechRarityTier.Silver;
		}

		if (HextechCatalog.GetPlayerRuneTypesForRarity(HextechRarityTier.Prismatic).Any(type => ModelDb.GetId(type) == id))
		{
			return HextechRarityTier.Prismatic;
		}

		return HextechRarityTier.Gold;
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
