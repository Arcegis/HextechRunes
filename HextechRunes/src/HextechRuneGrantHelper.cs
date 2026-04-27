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
		List<Type> pool = candidateTypes
			.Where(type => !ExcludedRewardRuneTypes.Contains(type))
			.Where(type => blockedIds == null || !blockedIds.Contains(ModelDb.GetId(type)))
			.Where(type =>
			{
				ModelId id = ModelDb.GetId(type);
				if (player.Relics.Any(relic => (relic.CanonicalInstance?.Id ?? relic.Id) == id))
				{
					return false;
				}

				RelicModel relic = ModelDb.GetById<RelicModel>(id);
				return ModInfo.IsAvailableForPlayer(relic, player);
			})
			.ToList();
		if (pool.Count == 0)
		{
			return;
		}

		int picks = Math.Min(count, pool.Count);
		for (int i = 0; i < picks; i++)
		{
			int index = player.RunState.Rng.Niche.NextInt(pool.Count);
			Type runeType = pool[index];
			pool.RemoveAt(index);

			RelicModel relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(runeType)).ToMutable();
			SaveManager.Instance.MarkRelicAsSeen(relic);
			await RelicCmd.Obtain(relic, player);
		}
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
