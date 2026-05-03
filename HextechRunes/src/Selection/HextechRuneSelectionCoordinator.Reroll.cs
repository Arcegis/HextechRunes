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
		IReadOnlyList<RelicModel> rerolled = RerollSingleOption(player, (RunState)player.RunState, currentOptions, slotIndex, seenOptionIds);
		if (!ReferenceEquals(rerolled, currentOptions))
		{
			ModelId rerolledId = rerolled[slotIndex].CanonicalInstance?.Id ?? rerolled[slotIndex].Id;
			seenOptionIds.Add(rerolledId);
			MarkRelicsSeen([ rerolled[slotIndex] ]);
			modifier.RecordSeenPlayerRunes(player, [ rerolled[slotIndex] ]);
		}

		return rerolled;
	}

	private static IReadOnlyList<RelicModel> RerollSingleOption(Player player, RunState runState, IReadOnlyList<RelicModel> currentOptions, int slotIndex, IReadOnlySet<ModelId> seenOptionIds)
	{
		if (slotIndex < 0 || slotIndex >= currentOptions.Count)
		{
			return currentOptions;
		}

		HashSet<ModelId> excludedIds = currentOptions
			.Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id)
			.ToHashSet();
		excludedIds.UnionWith(seenOptionIds);
		List<RelicModel> rerolled = BuildSelectableRunesForRarity(player, GetRarityForOptions(currentOptions), runState, excludedIds);
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
		IReadOnlyList<RelicModel> rerolled = RerollSingleOptionMultiplayer(player, currentOptions, slotIndex, rerollOrdinal, seenOptionIds);
		if (!ReferenceEquals(rerolled, currentOptions))
		{
			ModelId rerolledId = rerolled[slotIndex].CanonicalInstance?.Id ?? rerolled[slotIndex].Id;
			seenOptionIds.Add(rerolledId);
			MarkRelicsSeen([ rerolled[slotIndex] ]);
			modifier.RecordSeenPlayerRunes(player, [ rerolled[slotIndex] ]);
			Log.Info($"[{ModInfo.Id}][Mayhem] RerollSingleOptionMultiplayer: player={player.NetId} slot={slotIndex} ordinal={rerollOrdinal} relic={rerolledId.Entry}");
		}

		return rerolled;
	}

	private static IReadOnlyList<RelicModel> RerollSingleOptionMultiplayer(Player player, IReadOnlyList<RelicModel> currentOptions, int slotIndex, int rerollOrdinal, IReadOnlySet<ModelId> seenOptionIds)
	{
		if (slotIndex < 0 || slotIndex >= currentOptions.Count)
		{
			return currentOptions;
		}

		HashSet<ModelId> excludedIds = currentOptions
			.Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id)
			.ToHashSet();
		excludedIds.UnionWith(seenOptionIds);

		HextechRarityTier rarity = GetRarityForOptions(currentOptions);
		RunState runState = (RunState)player.RunState;
		List<RelicModel> pool = BuildSelectableRunePool(player, rarity, runState, excludedIds)
			.OrderBy(static relic => (relic.CanonicalInstance?.Id ?? relic.Id).Entry, StringComparer.Ordinal)
			.ToList();
		if (pool.Count == 0)
		{
			return currentOptions;
		}

		int index = GetMultiplayerRerollIndex(player, pool, rarity, slotIndex, rerollOrdinal);
		List<RelicModel> updated = currentOptions.ToList();
		updated[slotIndex] = pool[index].ToMutable();
		return updated;
	}

	private static int GetMultiplayerRerollIndex(Player player, IReadOnlyList<RelicModel> pool, HextechRarityTier rarity, int slotIndex, int rerollOrdinal)
	{
		RunState runState = (RunState)player.RunState;
		List<string> parts =
		[
			runState.Rng.StringSeed,
			"|act:",
			runState.CurrentActIndex.ToString(),
			"|player:",
			runState.GetPlayerSlotIndex(player).ToString(),
			"|net:",
			player.NetId.ToString(),
			"|rarity:",
			((int)rarity).ToString(),
			"|slot:",
			slotIndex.ToString(),
			"|ordinal:",
			rerollOrdinal.ToString()
		];
		foreach (RelicModel relic in pool)
		{
			parts.Add("|pool:");
			parts.Add((relic.CanonicalInstance?.Id ?? relic.Id).Entry);
		}

		return HextechStableRandom.IndexFromRawParts(pool.Count, parts.ToArray());
	}
}
