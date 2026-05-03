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

public sealed class RandomForgeShopRelic : HextechRelicBase
{
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedPurchaseCount
	{
		get => PurchaseCount;
		set => PurchaseCount = Math.Max(0, value);
	}

	public int PurchaseCount { get; private set; }

	public override bool IsAvailableForPlayer(Player player)
	{
		return false;
	}

	public void IncrementPurchaseCount()
	{
		PurchaseCount++;
	}
}

internal static class HextechForgeGrantHelper
{
	public static async Task ObtainRandomForges(Player player, int count)
	{
		for (int i = 0; i < count; i++)
		{
			if (!TryCreateStableRandomForge(player, "obtain-random-forges", i, out RelicModel? forge) || forge == null)
			{
				return;
			}

			SaveManager.Instance.MarkRelicAsSeen(forge);
			await RelicCmd.Obtain(forge, player);
		}
	}

	public static bool AddRandomForgeReward(Player player, CombatRoom room)
	{
		if (!TryCreateStableRandomForge(player, "combat-random-forge-reward", 0, out RelicModel? forge) || forge == null)
		{
			return false;
		}

		SaveManager.Instance.MarkRelicAsSeen(forge);
		room.AddExtraReward(player, new RelicReward(forge, player));
		return true;
	}

	public static bool AddRandomForgeReward(Player player, CombatRoom room, HextechRarityTier rarity)
	{
		if (!TryCreateStableRandomForge(player, rarity, "combat-random-forge-reward-rarity", 0, out RelicModel? forge) || forge == null)
		{
			return false;
		}

		SaveManager.Instance.MarkRelicAsSeen(forge);
		room.AddExtraReward(player, new RelicReward(forge, player));
		return true;
	}

	internal static bool TryCreateRandomForge(Player player, Rng rng, out RelicModel? forge)
	{
		HextechRarityTier rarity = RollForgeRarity(rng);
		return TryCreateRandomForge(player, rarity, rng, out forge);
	}

	private static bool TryCreateStableRandomForge(Player player, string source, int ordinal, out RelicModel? forge)
	{
		HextechRarityTier rarity = RollStableForgeRarity(player, source, ordinal);
		return TryCreateStableRandomForge(player, rarity, source, ordinal, out forge);
	}

	private static bool TryCreateStableRandomForge(Player player, HextechRarityTier rarity, string source, int ordinal, out RelicModel? forge)
	{
		List<Type> pool = BuildAvailableForgePool(player, HextechCatalog.GetForgeTypesForRarity(rarity));
		if (pool.Count == 0)
		{
			pool = BuildAvailableForgePool(player, HextechCatalog.GetAllForgeTypes());
		}

		if (pool.Count == 0)
		{
			forge = null;
			return false;
		}

		Type forgeType = HextechStableRandom.Pick(
			pool,
			(RunState)player.RunState,
			HextechStableRandom.TypeModelKey,
			source,
			HextechStableRandom.PlayerKey(player),
			ordinal.ToString(),
			((int)rarity).ToString(),
			player.Relics.Count.ToString());
		forge = ModelDb.GetById<RelicModel>(ModelDb.GetId(forgeType)).ToMutable();
		return true;
	}

	private static bool TryCreateRandomForge(Player player, HextechRarityTier rarity, Rng rng, out RelicModel? forge)
	{
		List<Type> pool = BuildAvailableForgePool(player, HextechCatalog.GetForgeTypesForRarity(rarity));
		if (pool.Count == 0)
		{
			pool = BuildAvailableForgePool(player, HextechCatalog.GetAllForgeTypes());
		}

		if (pool.Count == 0)
		{
			forge = null;
			return false;
		}

		Type forgeType = pool[rng.NextInt(pool.Count)];
		forge = ModelDb.GetById<RelicModel>(ModelDb.GetId(forgeType)).ToMutable();
		return true;
	}

	private static List<Type> BuildAvailableForgePool(Player player, IEnumerable<Type> candidateTypes)
	{
		return candidateTypes
			.Where(type => HextechCatalog.IsAvailableForPlayer(ModelDb.GetById<RelicModel>(ModelDb.GetId(type)), player))
			.ToList();
	}

	private static HextechRarityTier RollForgeRarity(Rng rng)
	{
		int roll = rng.NextInt(100);
		if (roll < 65)
		{
			return HextechRarityTier.Silver;
		}

		if (roll < 90)
		{
			return HextechRarityTier.Gold;
		}

		return HextechRarityTier.Prismatic;
	}

	private static HextechRarityTier RollStableForgeRarity(Player player, string source, int ordinal)
	{
		int roll = HextechStableRandom.Index(
			(RunState)player.RunState,
			100,
			source,
			"forge-rarity",
			HextechStableRandom.PlayerKey(player),
			ordinal.ToString(),
			player.Relics.Count.ToString());
		if (roll < 65)
		{
			return HextechRarityTier.Silver;
		}

		if (roll < 90)
		{
			return HextechRarityTier.Gold;
		}

		return HextechRarityTier.Prismatic;
	}
}
