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
	private static IReadOnlyList<RelicModel> RerollSingleOptionAndTrack(HextechMayhemModifier modifier, Player player, IReadOnlyList<RelicModel> currentOptions, int slotIndex, HashSet<ModelId> seenOptionIds)
	{
		IReadOnlyList<RelicModel> rerolled = RerollSingleOption(player, (RunState)player.RunState, currentOptions, slotIndex, seenOptionIds, modifier.IsEndlessLoopActive);
		if (!ReferenceEquals(rerolled, currentOptions))
		{
			ModelId rerolledId = rerolled[slotIndex].CanonicalInstance?.Id ?? rerolled[slotIndex].Id;
			seenOptionIds.Add(rerolledId);
			MarkRelicsSeen([ rerolled[slotIndex] ]);
			modifier.RecordSeenPlayerRunes(player, [ rerolled[slotIndex] ]);
		}

		return rerolled;
	}

	private static IReadOnlyList<RelicModel> RerollSingleOption(
		Player player,
		RunState runState,
		IReadOnlyList<RelicModel> currentOptions,
		int slotIndex,
		HashSet<ModelId> seenOptionIds,
		bool useEndlessTagWindow)
	{
		if (slotIndex < 0 || slotIndex >= currentOptions.Count)
		{
			return currentOptions;
		}

		HashSet<ModelId> currentOptionIds = currentOptions
			.Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id)
			.ToHashSet();
		HashSet<ModelId> excludedIds = new(currentOptionIds);
		excludedIds.UnionWith(seenOptionIds);
		HextechRarityTier rarity = GetRarityForOptions(currentOptions);
		List<RelicModel> rerolled = BuildSelectableRunesForRarity(player, rarity, runState, excludedIds, useEndlessTagWindow);
		if (rerolled.Count == 0 && seenOptionIds.Count > 0)
		{
			// 池被「已见」清空:重置(清空)已见集,让重随能重新刷到此前见过的符文(仍排除当前选项)。
			seenOptionIds.Clear();
			rerolled = BuildSelectableRunesForRarity(player, rarity, runState, currentOptionIds, useEndlessTagWindow);
		}

		if (rerolled.Count == 0)
		{
			return currentOptions;
		}

		List<RelicModel> updated = currentOptions.ToList();
		updated[slotIndex] = rerolled[0];
		return updated;
	}

	private static IReadOnlyList<RelicModel> RerollSingleOptionAndTrackMultiplayer(HextechMayhemModifier modifier, Player player, IReadOnlyList<RelicModel> currentOptions, int slotIndex, int rerollOrdinal, HashSet<ModelId> seenOptionIds)
	{
		IReadOnlyList<RelicModel> rerolled = RerollSingleOptionMultiplayer(player, currentOptions, slotIndex, rerollOrdinal, seenOptionIds, modifier.IsEndlessLoopActive);
		if (!ReferenceEquals(rerolled, currentOptions))
		{
			ModelId rerolledId = rerolled[slotIndex].CanonicalInstance?.Id ?? rerolled[slotIndex].Id;
			seenOptionIds.Add(rerolledId);
			MarkRelicsSeen([ rerolled[slotIndex] ]);
			modifier.RecordSeenPlayerRunes(player, [ rerolled[slotIndex] ]);
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] RerollSingleOptionMultiplayer: player={player.NetId} slot={slotIndex} ordinal={rerollOrdinal} relic={rerolledId.Entry}");
		}

		return rerolled;
	}

	private static IReadOnlyList<RelicModel> RerollSingleOptionMultiplayer(
		Player player,
		IReadOnlyList<RelicModel> currentOptions,
		int slotIndex,
		int rerollOrdinal,
		HashSet<ModelId> seenOptionIds,
		bool useEndlessTagWindow)
	{
		if (slotIndex < 0 || slotIndex >= currentOptions.Count)
		{
			return currentOptions;
		}

		HashSet<ModelId> currentOptionIds = currentOptions
			.Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id)
			.ToHashSet();
		HashSet<ModelId> excludedIds = new(currentOptionIds);
		excludedIds.UnionWith(seenOptionIds);

		HextechRarityTier rarity = GetRarityForOptions(currentOptions);
		RunState runState = (RunState)player.RunState;
		List<RelicModel> pool = BuildSelectableRunePool(player, rarity, runState, excludedIds)
			.OrderBy(static relic => (relic.CanonicalInstance?.Id ?? relic.Id).Entry, StringComparer.Ordinal)
			.ToList();
		if (pool.Count == 0 && seenOptionIds.Count > 0)
		{
			// 池被「已见」清空:重置(清空)已见集,让重随能重新刷到此前见过的符文(仍排除当前选项)。
			seenOptionIds.Clear();
			pool = BuildSelectableRunePool(player, rarity, runState, currentOptionIds)
				.OrderBy(static relic => (relic.CanonicalInstance?.Id ?? relic.Id).Entry, StringComparer.Ordinal)
				.ToList();
		}

		if (pool.Count == 0)
		{
			return currentOptions;
		}

		int index = GetMultiplayerRerollIndex(player, pool, rarity, slotIndex, rerollOrdinal, useEndlessTagWindow);
		List<RelicModel> updated = currentOptions.ToList();
		updated[slotIndex] = CreateSelectableRuneOption(player, pool[index]);
		return updated;
	}

	private static int GetMultiplayerRerollIndex(
		Player player,
		IReadOnlyList<RelicModel> pool,
		HextechRarityTier rarity,
		int slotIndex,
		int rerollOrdinal,
		bool useEndlessTagWindow)
	{
		RunState runState = (RunState)player.RunState;
		Dictionary<string, int> tagCounts = BuildOwnedRuneTagCounts(player, useEndlessTagWindow);
		List<int> weights = BuildRuneTagWeights(pool, tagCounts, useEndlessTagWindow, out int totalWeight);
		List<string> parts =
		[
			runState.Rng.StringSeed,
			"|act:",
			runState.CurrentActIndex.ToString(),
			"|player:",
			HextechStableRandom.PlayerKey(player),
			"|rarity:",
			((int)rarity).ToString(),
			"|slot:",
			slotIndex.ToString(),
			"|ordinal:",
			rerollOrdinal.ToString()
		];
		for (int i = 0; i < pool.Count; i++)
		{
			parts.Add("|pool:");
			parts.Add((pool[i].CanonicalInstance?.Id ?? pool[i].Id).Entry);
			parts.Add(":");
			parts.Add(weights[i].ToString());
		}

		int roll = HextechStableRandom.IndexFromRawParts(totalWeight, parts.ToArray());
		return SelectWeightedIndex(weights, roll);
	}
}
