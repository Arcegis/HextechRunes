using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using CoreHook = MegaCrit.Sts2.Core.Hooks.Hook;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechShopForgeHooks
{
	private const int RandomForgeShopRegularCost = 250;
	private const float CardRemovalRandomForgeOffsetY = 60f;

	private static readonly FieldInfo? MerchantInventoryRelicEntriesField = TryGetField(typeof(MerchantInventory), "_relicEntries");
	private static readonly Dictionary<ulong, Vector2> CardRemovalOriginalPositions = [];

	public static void Install(Harmony harmony)
	{
		bool purchaseHookInstalled = TryPatch(
			harmony,
			() => RequireMethodAllowingSingleArityFallback(typeof(MerchantRelicEntry), "OnTryPurchase", BindingFlags.Instance | BindingFlags.NonPublic, typeof(MerchantInventory), typeof(bool)),
			"random forge purchase",
			prefix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(MerchantRelicPurchasePrefix)));
		bool restockHookInstalled = TryPatch(
			harmony,
			() => RequireMethodAllowingSingleArityFallback(typeof(MerchantRelicEntry), "RestockAfterPurchase", BindingFlags.Instance | BindingFlags.NonPublic, typeof(MerchantInventory)),
			"random forge restock",
			prefix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(MerchantRelicRestockPrefix)));
		bool priceHookInstalled = TryPatch(
			harmony,
			() => RequireMethodAllowingSingleArityFallback(typeof(CoreHook), nameof(CoreHook.ModifyMerchantPrice), BindingFlags.Static | BindingFlags.Public, typeof(IRunState), typeof(Player), typeof(MerchantEntry), typeof(decimal)),
			"random forge price",
			prefix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(ModifyMerchantPricePrefix)));
		bool refillHookInstalled = TryPatch(
			harmony,
			() => RequireMethodAllowingSingleArityFallback(typeof(CoreHook), nameof(CoreHook.ShouldRefillMerchantEntry), BindingFlags.Static | BindingFlags.Public, typeof(IRunState), typeof(MerchantEntry), typeof(Player)),
			"random forge refill",
			prefix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(ShouldRefillMerchantEntryPrefix)));

		if (!purchaseHookInstalled || !restockHookInstalled || !priceHookInstalled || !refillHookInstalled)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Random forge shop entry disabled because one or more merchant hooks are unavailable.");
			return;
		}

		TryPatch(
			harmony,
			() => RequireMethodAllowingSingleArityFallback(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant), BindingFlags.Static | BindingFlags.Public, typeof(Player)),
			"random forge merchant entry",
			postfix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(CreateForNormalMerchantPostfix)));
		TryPatch(
			harmony,
			() => RequireMethodAllowingSingleArityFallback(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize), BindingFlags.Instance | BindingFlags.Public, typeof(MerchantInventory), typeof(MerchantDialogueSet)),
			"random forge shop layout",
			prefix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(MerchantInventoryInitializePrefix)),
			postfix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(MerchantInventoryInitializePostfix)));
		TryPatch(
			harmony,
			() => RequireMethod(typeof(NMerchantRelic), "OnSuccessfulPurchase", BindingFlags.Instance | BindingFlags.NonPublic, typeof(PurchaseStatus), typeof(MerchantEntry)),
			"random forge purchase animation",
			prefix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(MerchantRelicSuccessfulPurchasePrefix)));
	}

	private static bool TryPatch(Harmony harmony, Func<MethodInfo> resolveTarget, string label, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null)
	{
		try
		{
			MethodInfo target = resolveTarget();
			harmony.Patch(target, prefix, postfix);
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Shop random forge hook skipped: {label}: {ex.GetType().Name}: {ex.Message}");
			return false;
		}
	}

	private static void CreateForNormalMerchantPostfix(Player player, MerchantInventory __result)
	{
		InstallRandomForgeEntry(__result, player);
	}

	private static void ModifyMerchantPricePrefix(MerchantEntry entry, ref decimal result)
	{
		if (TryGetRandomForgeShopRelic(entry, out RandomForgeShopRelic? shopRelic) && shopRelic != null)
		{
			HextechForgeShopPriceHelper.RefreshRandomForgeShopRelic(shopRelic, shopRelic.Owner?.RunState as RunState);
			result = GetRandomForgeShopBaseCost(shopRelic);
		}
	}

	private static bool ShouldRefillMerchantEntryPrefix(MerchantEntry entry, ref bool __result)
	{
		if (!IsRandomForgeEntry(entry))
		{
			return true;
		}

		__result = true;
		return false;
	}

	private static void MerchantInventoryInitializePrefix(NMerchantInventory __instance, MerchantInventory inventory)
	{
		if (IsFakeMerchantInventory(__instance))
		{
			RemoveRandomForgeEntries(inventory);
			return;
		}

		InstallRandomForgeEntry(inventory, inventory.Player);
		EnsureRandomForgeRelicSlot(__instance, inventory);
	}

	private static void MerchantInventoryInitializePostfix(NMerchantInventory __instance, MerchantInventory inventory)
	{
		if (IsFakeMerchantInventory(__instance))
		{
			return;
		}

		MoveCardRemovalBelowRandomForge(__instance, inventory);
	}

	private static bool MerchantRelicPurchasePrefix(MerchantRelicEntry __instance, MerchantInventory inventory, bool ignoreCost, ref Task<(bool, int)> __result)
	{
		if (!IsRandomForgeEntry(__instance))
		{
			return true;
		}

		__result = PurchaseRandomForge(__instance, inventory, ignoreCost);
		return false;
	}

	private static bool MerchantRelicRestockPrefix(MerchantRelicEntry __instance)
	{
		return !IsRandomForgeEntry(__instance);
	}

	private static bool MerchantRelicSuccessfulPurchasePrefix(NMerchantRelic __instance)
	{
		if (!IsRandomForgeEntry(__instance.Entry))
		{
			return true;
		}

		__instance.Entry.OnMerchantInventoryUpdated();
		HextechLog.Info($"[{ModInfo.Id}][Mayhem] Skipped merchant relic inventory animation for random forge placeholder.");
		return false;
	}

	private static void InstallRandomForgeEntry(MerchantInventory inventory, Player player)
	{
		if (inventory.RelicEntries.Any(IsRandomForgeEntry))
		{
			return;
		}

		RandomForgeShopRelic shopRelic = (RandomForgeShopRelic)ModelDb.Relic<RandomForgeShopRelic>().ToMutable();
		HextechForgeShopPriceHelper.RefreshRandomForgeShopRelic(shopRelic, player.RunState as RunState);
		MerchantRelicEntry entry = new(shopRelic, player);
		entry.PurchaseCompleted += (_, _) => UpdateInventoryEntries(inventory);
		inventory.AddRelicEntry(entry);
	}

	private static async Task<(bool, int)> PurchaseRandomForge(MerchantRelicEntry entry, MerchantInventory inventory, bool ignoreCost)
	{
		Player player = inventory.Player;
		if (TryGetRandomForgeShopRelic(entry, out RandomForgeShopRelic? activeShopRelic) && activeShopRelic != null)
		{
			HextechForgeShopPriceHelper.RefreshRandomForgeShopRelic(activeShopRelic, player.RunState as RunState);
		}

		int cost = TryGetRandomForgeShopRelic(entry, out RandomForgeShopRelic? shopRelic) && shopRelic != null
			? entry.Cost
			: RandomForgeShopRegularCost;

		int purchaseOrdinal = shopRelic?.PurchaseCount ?? 0;
		if (!HextechForgeGrantHelper.TryCreateStableShopForgeChoice(player, purchaseOrdinal, out List<RelicModel> options))
		{
#if STS2_104_OR_NEWER
			entry.InvokePurchaseFailed(PurchaseStatus.FailureOutOfStock);
#else
			entry.InvokePurchaseFailed(PurchaseStatus.FailureForbidden);
#endif
			return (false, 0);
		}

		RelicModel? forge = await HextechForgeSelectionCoordinator.SelectForge(player, options, "shop", syncMultiplayerChoice: false);
		if (forge == null)
		{
			return (false, 0);
		}

		if (!CanContinueSynchronizedPurchase())
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Random forge purchase cancelled because multiplayer service is disconnected.");
			return (false, 0);
		}

		if (!ignoreCost)
		{
			await PlayerCmd.LoseGold(cost, player, GoldLossType.Spent);
			if (CanSyncMultiplayerReward())
			{
				RunManager.Instance.RewardSynchronizer.SyncLocalGoldLost(cost);
			}
		}

		player.RunState.CurrentMapPointHistoryEntry?
			.GetEntry(player.NetId)
			.BoughtRelics
			.Add(forge.Id);

		await HextechForgeGrantHelper.ObtainSelectedForge(player, forge, syncObtainedRelic: true);
		if (shopRelic != null)
		{
			shopRelic.IncrementPurchaseCount();
			entry.OnMerchantInventoryUpdated();
		}
		return (true, ignoreCost ? 0 : cost);
	}

	private static bool CanContinueSynchronizedPurchase()
	{
		INetGameService netService = RunManager.Instance.NetService;
		return netService.Type is not (NetGameType.Host or NetGameType.Client) || netService.IsConnected;
	}

	private static bool CanSyncMultiplayerReward()
	{
		INetGameService netService = RunManager.Instance.NetService;
		return netService.Type is NetGameType.Host or NetGameType.Client && netService.IsConnected;
	}

	private static bool IsRandomForgeEntry(MerchantEntry entry)
	{
		return entry is MerchantRelicEntry relicEntry && HextechCatalog.IsHextechShopRelic(relicEntry.Model);
	}

	private static bool IsFakeMerchantInventory(NMerchantInventory merchantInventory)
	{
		return merchantInventory is NFakeMerchantInventory;
	}

	private static void RemoveRandomForgeEntries(MerchantInventory inventory)
	{
		if (!inventory.RelicEntries.Any(IsRandomForgeEntry))
		{
			return;
		}

		if (MerchantInventoryRelicEntriesField?.GetValue(inventory) is List<MerchantRelicEntry> relicEntries)
		{
			relicEntries.RemoveAll(IsRandomForgeEntry);
		}
	}

	private static bool TryGetRandomForgeShopRelic(MerchantEntry entry, out RandomForgeShopRelic? shopRelic)
	{
		shopRelic = entry is MerchantRelicEntry relicEntry ? relicEntry.Model as RandomForgeShopRelic : null;
		return shopRelic != null;
	}

	private static int GetRandomForgeShopBaseCost(RandomForgeShopRelic shopRelic)
	{
		return HextechForgeShopPriceHelper.GetRandomForgeShopPriceFor(shopRelic.Owner?.RunState as RunState);
	}

	private static void UpdateInventoryEntries(MerchantInventory inventory)
	{
		foreach (MerchantEntry entry in inventory.AllEntries)
		{
			if (TryGetRandomForgeShopRelic(entry, out RandomForgeShopRelic? shopRelic) && shopRelic != null)
			{
				HextechForgeShopPriceHelper.RefreshRandomForgeShopRelic(shopRelic, inventory.Player.RunState as RunState);
			}

			entry.OnMerchantInventoryUpdated();
		}
	}

	private static void EnsureRandomForgeRelicSlot(NMerchantInventory merchantInventory, MerchantInventory inventory)
	{
		if (!inventory.RelicEntries.Any(IsRandomForgeEntry))
		{
			return;
		}

		if (merchantInventory.GetNodeOrNull<Control>("%Relics") is not Control relicContainer)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Random forge shop slot skipped: relic container unavailable.");
			return;
		}

		List<NMerchantRelic> relicSlots = relicContainer.GetChildren().OfType<NMerchantRelic>().ToList();
		while (relicSlots.Count < inventory.RelicEntries.Count)
		{
			NMerchantRelic? template = relicSlots.LastOrDefault();
			if (template == null)
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem] Random forge shop slot skipped: no relic slot template available.");
				return;
			}

			Node duplicatedNode = template.Duplicate();
			if (duplicatedNode is not NMerchantRelic extraSlot)
			{
				duplicatedNode.QueueFree();
				Log.Warn($"[{ModInfo.Id}][Mayhem] Random forge shop slot skipped: duplicated node is not a merchant relic slot.");
				return;
			}

			extraSlot.Name = $"{template.Name}_HextechExtra{relicSlots.Count}";
			extraSlot.Position = template.Position + GetNextSlotOffset(relicSlots);
			relicContainer.AddChild(extraSlot);
			relicSlots.Add(extraSlot);
		}
	}

	private static void MoveCardRemovalBelowRandomForge(NMerchantInventory merchantInventory, MerchantInventory inventory)
	{
		if (!inventory.RelicEntries.Any(IsRandomForgeEntry))
		{
			return;
		}

		object? cardRemovalNode = merchantInventory.GetNodeOrNull<NMerchantCardRemoval>("%MerchantCardRemoval");
		if (!TryMoveCardRemovalNode(cardRemovalNode, new Vector2(0f, CardRemovalRandomForgeOffsetY)))
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Random forge shop card removal offset skipped: card removal node unavailable.");
		}
	}

	private static bool TryMoveCardRemovalNode(object? cardRemovalNode, Vector2 offset)
	{
		switch (cardRemovalNode)
		{
			case Control control:
				control.Position = GetOriginalCardRemovalPosition(control, control.Position) + offset;
				return true;
			case Node2D node:
				node.Position = GetOriginalCardRemovalPosition(node, node.Position) + offset;
				return true;
			default:
				return false;
		}
	}

	private static Vector2 GetOriginalCardRemovalPosition(GodotObject node, Vector2 currentPosition)
	{
		ulong instanceId = node.GetInstanceId();
		if (!CardRemovalOriginalPositions.TryGetValue(instanceId, out Vector2 originalPosition))
		{
			originalPosition = currentPosition;
			CardRemovalOriginalPositions[instanceId] = originalPosition;
		}

		return originalPosition;
	}

	private static Vector2 GetNextSlotOffset(IReadOnlyList<NMerchantRelic> relicSlots)
	{
		if (relicSlots.Count >= 2)
		{
			Vector2 offset = relicSlots[^1].Position - relicSlots[^2].Position;
			if (offset.LengthSquared() > 1f)
			{
				return offset;
			}
		}

		return new Vector2(160f, 0f);
	}

}
