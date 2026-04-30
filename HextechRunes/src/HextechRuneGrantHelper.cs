using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

internal static class HextechRuneGrantHelper
{
	private static readonly IReadOnlySet<Type> ExcludedRewardRuneTypes = new HashSet<Type>
	{
		typeof(TransmuteChaosRune),
		typeof(TransmutePrismaticRune),
		typeof(TransmuteGoldRune)
	};

	public static async Task ObtainRandomRunes(Player player, IEnumerable<Type> candidateTypes, int count)
	{
		await ObtainRandomRunes(player, candidateTypes, count, blockedIds: null);
	}

	public static async Task ObtainRandomRunes(Player player, IEnumerable<Type> candidateTypes, int count, IReadOnlySet<ModelId>? blockedIds)
	{
		IReadOnlyList<Type> candidates = candidateTypes as IReadOnlyList<Type> ?? candidateTypes.ToArray();
		HashSet<ModelId> selectedIds = new();
		for (int i = 0; i < count; i++)
		{
			List<Type> pool = BuildObtainableRunePool(player, candidates, blockedIds, selectedIds);
			if (pool.Count == 0)
			{
				return;
			}

			int index = player.RunState.Rng.Niche.NextInt(pool.Count);
			Type runeType = pool[index];
			ModelId runeId = ModelDb.GetId(runeType);
			selectedIds.Add(runeId);

			RelicModel relic = ModelDb.GetById<RelicModel>(runeId).ToMutable();
			SaveManager.Instance.MarkRelicAsSeen(relic);
			await RelicCmd.Obtain(relic, player);
		}
	}

	private static List<Type> BuildObtainableRunePool(
		Player player,
		IEnumerable<Type> candidateTypes,
		IReadOnlySet<ModelId>? blockedIds,
		IReadOnlySet<ModelId> selectedIds)
	{
		HashSet<ModelId> ownedAndSelectedIds = player.Relics
			.Where(ModInfo.IsHextechRelic)
			.Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id)
			.Concat(selectedIds)
			.ToHashSet();
		HashSet<ModelId> unavailableIds = ownedAndSelectedIds.ToHashSet();
		unavailableIds.UnionWith(ModInfo.GetMutuallyExclusivePlayerRuneIds(ownedAndSelectedIds));
		if (blockedIds != null)
		{
			unavailableIds.UnionWith(blockedIds);
		}

		return candidateTypes
			.Where(type => !ExcludedRewardRuneTypes.Contains(type))
			.Where(type => !unavailableIds.Contains(ModelDb.GetId(type)))
			.Where(type =>
			{
				RelicModel relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(type));
				return ModInfo.IsAvailableForPlayer(relic, player);
			})
			.ToList();
	}

	public static async Task ReplaceOwnedHextechRunesWithRandomRunes(Player player, IEnumerable<Type> candidateTypes, IReadOnlySet<ModelId>? blockedIds = null)
	{
		List<RelicModel> ownedRunes = player.Relics.Where(ModInfo.IsHextechRelic).ToList();
		if (ownedRunes.Count == 0)
		{
			return;
		}

		foreach (RelicModel relic in ownedRunes)
		{
			await RelicCmd.Remove(relic);
		}

		await ObtainRandomRunes(player, candidateTypes, ownedRunes.Count, blockedIds);
	}

	public static async Task ConsumeAndObtainRandomRunes(RelicModel consumedRune, Player player, IEnumerable<Type> candidateTypes, int count)
	{
		await RelicCmd.Remove(consumedRune);
		await ObtainRandomRunes(player, candidateTypes, count);
	}
}
