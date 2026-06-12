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
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Game;
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
			if (!TryCreateStableRandomForgeChoice(player, "obtain-random-forges", i, out List<RelicModel> options))
			{
				return;
			}

			RelicModel? selected = await HextechForgeSelectionCoordinator.SelectForge(player, options, $"obtain-random-forges:{i}");
			if (selected == null)
			{
				return;
			}

			await ObtainSelectedForge(player, selected, syncObtainedRelic: false);
		}
	}

	public static bool AddRandomForgeReward(Player player, CombatRoom room)
	{
		if (!TryCreateStableRandomForgeChoice(player, "combat-random-forge-reward", 0, out List<RelicModel> options))
		{
			return false;
		}

		room.AddExtraReward(player, new HextechForgeChoiceReward(options, player));
		return true;
	}

	public static bool AddWeightedRandomForgeReward(
		Player player,
		CombatRoom room,
		string source,
		int silverWeight,
		int goldWeight,
		int prismaticWeight)
	{
		if (!TryCreateStableRandomForgeChoice(player, source, 0, silverWeight, goldWeight, prismaticWeight, out List<RelicModel> options))
		{
			return false;
		}

		room.AddExtraReward(player, new HextechForgeChoiceReward(options, player));
		return true;
	}

	public static bool AddRandomForgeReward(Player player, CombatRoom room, HextechRarityTier rarity)
	{
		if (!TryCreateStableRandomForgeChoice(player, rarity, "combat-random-forge-reward-rarity", 0, out List<RelicModel> options))
		{
			return false;
		}

		room.AddExtraReward(player, new HextechForgeChoiceReward(options, player));
		return true;
	}

	public static async Task ObtainSelectedForge(Player player, RelicModel forge, bool syncObtainedRelic)
	{
		SaveManager.Instance.MarkRelicAsSeen(forge);
		await RelicCmd.Obtain(forge, player);
		if (syncObtainedRelic)
		{
			INetGameService netService = RunManager.Instance.NetService;
			if (netService.Type is NetGameType.Host or NetGameType.Client && netService.IsConnected)
			{
				RunManager.Instance.RewardSynchronizer.SyncLocalObtainedRelic(forge);
			}
			else if (netService.Type is NetGameType.Host or NetGameType.Client)
			{
				Log.Warn($"[{ModInfo.Id}][ForgeChoice] Skipped forge reward sync because multiplayer service is disconnected: relic={forge.Id.Entry}");
			}
		}
	}

	internal static bool TryCreateRandomForge(Player player, Rng rng, out RelicModel? forge)
	{
		HextechRarityTier rarity = RollForgeRarity(rng);
		return TryCreateRandomForge(player, rarity, rng, out forge);
	}

	internal static bool TryCreateRandomForgeChoice(Player player, Rng rng, out List<RelicModel> options)
	{
		HextechRarityTier rarity = RollForgeRarity(rng);
		return TryCreateRandomForgeChoice(player, rarity, rng, out options);
	}

	internal static bool TryCreateStableShopForgeChoice(Player player, int purchaseOrdinal, out List<RelicModel> options)
	{
		return TryCreateStableRandomForgeChoice(player, "shop-random-forge", purchaseOrdinal, out options);
	}

	private static bool TryCreateStableRandomForge(Player player, string source, int ordinal, out RelicModel? forge)
	{
		HextechRarityTier rarity = RollStableForgeRarity(player, source, ordinal);
		return TryCreateStableRandomForge(player, rarity, source, ordinal, out forge);
	}

	private static bool TryCreateStableRandomForgeChoice(Player player, string source, int ordinal, out List<RelicModel> options)
	{
		HextechRarityTier rarity = RollStableForgeRarity(player, source, ordinal);
		return TryCreateStableRandomForgeChoice(player, rarity, source, ordinal, out options);
	}

	private static bool TryCreateStableRandomForge(
		Player player,
		string source,
		int ordinal,
		int silverWeight,
		int goldWeight,
		int prismaticWeight,
		out RelicModel? forge)
	{
		HextechRarityTier rarity = RollStableForgeRarity(player, source, ordinal, silverWeight, goldWeight, prismaticWeight);
		return TryCreateStableRandomForge(player, rarity, source, ordinal, out forge);
	}

	private static bool TryCreateStableRandomForgeChoice(
		Player player,
		string source,
		int ordinal,
		int silverWeight,
		int goldWeight,
		int prismaticWeight,
		out List<RelicModel> options)
	{
		HextechRarityTier rarity = RollStableForgeRarity(player, source, ordinal, silverWeight, goldWeight, prismaticWeight);
		return TryCreateStableRandomForgeChoice(player, rarity, source, ordinal, out options);
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

	private static bool TryCreateStableRandomForgeChoice(Player player, HextechRarityTier rarity, string source, int ordinal, out List<RelicModel> options)
	{
		List<Type> pool = BuildAvailableForgePool(player, HextechCatalog.GetForgeTypesForRarity(rarity));
		if (pool.Count == 0)
		{
			pool = BuildAvailableForgePool(player, HextechCatalog.GetAllForgeTypes());
		}

		if (pool.Count == 0)
		{
			options = [];
			return false;
		}

		List<Type> forgeTypes = HextechStableRandom.PickDistinct(
			pool,
			Math.Min(3, pool.Count),
			(RunState)player.RunState,
			HextechStableRandom.TypeModelKey,
			source,
			"forge-choice",
			HextechStableRandom.PlayerKey(player),
			ordinal.ToString(),
			((int)rarity).ToString(),
			player.Relics.Count.ToString());
		options = forgeTypes
			.Select(static type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)).ToMutable())
			.ToList();
		return options.Count > 0;
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

	private static bool TryCreateRandomForgeChoice(Player player, HextechRarityTier rarity, Rng rng, out List<RelicModel> options)
	{
		List<Type> pool = BuildAvailableForgePool(player, HextechCatalog.GetForgeTypesForRarity(rarity));
		if (pool.Count == 0)
		{
			pool = BuildAvailableForgePool(player, HextechCatalog.GetAllForgeTypes());
		}

		if (pool.Count == 0)
		{
			options = [];
			return false;
		}

		List<RelicModel> selected = new(Math.Min(3, pool.Count));
		for (int i = 0; i < 3 && pool.Count > 0; i++)
		{
			int index = rng.NextInt(pool.Count);
			Type forgeType = pool[index];
			pool.RemoveAt(index);
			selected.Add(ModelDb.GetById<RelicModel>(ModelDb.GetId(forgeType)).ToMutable());
		}

		options = selected;
		return options.Count > 0;
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
		return RollStableForgeRarity(player, source, ordinal, 65, 25, 10);
	}

	private static HextechRarityTier RollStableForgeRarity(
		Player player,
		string source,
		int ordinal,
		int silverWeight,
		int goldWeight,
		int prismaticWeight)
	{
		silverWeight = Math.Max(0, silverWeight);
		goldWeight = Math.Max(0, goldWeight);
		prismaticWeight = Math.Max(0, prismaticWeight);
		int totalWeight = silverWeight + goldWeight + prismaticWeight;
		if (totalWeight <= 0)
		{
			return HextechRarityTier.Silver;
		}

		int roll = HextechStableRandom.Index(
			(RunState)player.RunState,
			totalWeight,
			source,
			"forge-rarity",
			HextechStableRandom.PlayerKey(player),
			ordinal.ToString(),
			player.Relics.Count.ToString(),
			silverWeight.ToString(),
			goldWeight.ToString(),
			prismaticWeight.ToString());
		if (roll < silverWeight)
		{
			return HextechRarityTier.Silver;
		}

		if (roll < silverWeight + goldWeight)
		{
			return HextechRarityTier.Gold;
		}

		return HextechRarityTier.Prismatic;
	}
}
