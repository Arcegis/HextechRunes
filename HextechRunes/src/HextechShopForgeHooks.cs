using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using CoreHook = MegaCrit.Sts2.Core.Hooks.Hook;

namespace HextechRunes;

internal static class HextechShopForgeHooks
{
	private const int RandomForgeShopFirstCost = 125;
	private const int RandomForgeShopRegularCost = 250;
	private const float CardRemovalRandomForgeOffsetY = 60f;

	private static readonly Dictionary<ulong, Vector2> CardRemovalOriginalPositions = [];

	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant), BindingFlags.Static | BindingFlags.Public, typeof(Player)),
			postfix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(CreateForNormalMerchantPostfix)));
		harmony.Patch(
			RequireMethod(typeof(MerchantRelicEntry), "OnTryPurchase", BindingFlags.Instance | BindingFlags.NonPublic, typeof(MerchantInventory), typeof(bool)),
			prefix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(MerchantRelicPurchasePrefix)));
		harmony.Patch(
			RequireMethod(typeof(MerchantRelicEntry), "RestockAfterPurchase", BindingFlags.Instance | BindingFlags.NonPublic, typeof(MerchantInventory)),
			prefix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(MerchantRelicRestockPrefix)));
		harmony.Patch(
			RequireMethod(typeof(CoreHook), nameof(CoreHook.ModifyMerchantPrice), BindingFlags.Static | BindingFlags.Public, typeof(IRunState), typeof(Player), typeof(MerchantEntry), typeof(decimal)),
			prefix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(ModifyMerchantPricePrefix)));
		harmony.Patch(
			RequireMethod(typeof(CoreHook), nameof(CoreHook.ShouldRefillMerchantEntry), BindingFlags.Static | BindingFlags.Public, typeof(IRunState), typeof(MerchantEntry), typeof(Player)),
			prefix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(ShouldRefillMerchantEntryPrefix)));
		harmony.Patch(
			RequireMethod(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize), BindingFlags.Instance | BindingFlags.Public, typeof(MerchantInventory), typeof(MerchantDialogueSet)),
			prefix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(MerchantInventoryInitializePrefix)),
			postfix: new HarmonyMethod(typeof(HextechShopForgeHooks), nameof(MerchantInventoryInitializePostfix)));
	}

	private static void CreateForNormalMerchantPostfix(Player player, MerchantInventory __result)
	{
		InstallRandomForgeEntry(__result, player);
	}

	private static bool ModifyMerchantPricePrefix(MerchantEntry entry, ref decimal __result)
	{
		if (TryGetRandomForgeShopRelic(entry, out RandomForgeShopRelic? shopRelic) && shopRelic != null)
		{
			__result = GetRandomForgeShopCost(shopRelic);
			return false;
		}

		return true;
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
		InstallRandomForgeEntry(inventory, inventory.Player);
		EnsureRandomForgeRelicSlot(__instance, inventory);
	}

	private static void MerchantInventoryInitializePostfix(NMerchantInventory __instance, MerchantInventory inventory)
	{
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

	private static void InstallRandomForgeEntry(MerchantInventory inventory, Player player)
	{
		if (inventory.RelicEntries.Any(IsRandomForgeEntry))
		{
			return;
		}

		MerchantRelicEntry entry = new(ModelDb.Relic<RandomForgeShopRelic>().ToMutable(), player);
		entry.PurchaseCompleted += (_, _) => UpdateInventoryEntries(inventory);
		inventory.AddRelicEntry(entry);
	}

	private static async Task<(bool, int)> PurchaseRandomForge(MerchantRelicEntry entry, MerchantInventory inventory, bool ignoreCost)
	{
		Player player = inventory.Player;
		int cost = TryGetRandomForgeShopRelic(entry, out RandomForgeShopRelic? shopRelic) && shopRelic != null
			? GetRandomForgeShopCost(shopRelic)
			: RandomForgeShopFirstCost;

		if (!HextechForgeGrantHelper.TryCreateRandomForge(player, player.PlayerRng.Shops, out RelicModel? forge) || forge == null)
		{
#if STS2_104_OR_NEWER
			entry.InvokePurchaseFailed(PurchaseStatus.FailureOutOfStock);
#else
			entry.InvokePurchaseFailed(PurchaseStatus.FailureForbidden);
#endif
			return (false, 0);
		}

		if (!ignoreCost)
		{
			await PlayerCmd.LoseGold(cost, player, GoldLossType.Spent);
			RunManager.Instance.RewardSynchronizer.SyncLocalGoldLost(cost);
		}

		player.RunState.CurrentMapPointHistoryEntry?
			.GetEntry(player.NetId)
			.BoughtRelics
			.Add(forge.Id);

		SaveManager.Instance.MarkRelicAsSeen(forge);
		await RelicCmd.Obtain(forge, player);
		RunManager.Instance.RewardSynchronizer.SyncLocalObtainedRelic(forge);
		if (shopRelic != null)
		{
			shopRelic.IncrementPurchaseCount();
			entry.OnMerchantInventoryUpdated();
		}
		return (true, ignoreCost ? 0 : cost);
	}

	private static bool IsRandomForgeEntry(MerchantEntry entry)
	{
		return entry is MerchantRelicEntry relicEntry && ModInfo.IsHextechShopRelic(relicEntry.Model);
	}

	private static bool TryGetRandomForgeShopRelic(MerchantEntry entry, out RandomForgeShopRelic? shopRelic)
	{
		shopRelic = entry is MerchantRelicEntry relicEntry ? relicEntry.Model as RandomForgeShopRelic : null;
		return shopRelic != null;
	}

	private static int GetRandomForgeShopCost(RandomForgeShopRelic shopRelic)
	{
		return shopRelic.PurchaseCount == 0 ? RandomForgeShopFirstCost : RandomForgeShopRegularCost;
	}

	private static void UpdateInventoryEntries(MerchantInventory inventory)
	{
		foreach (MerchantEntry entry in inventory.AllEntries)
		{
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

	private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] parameters)
	{
		MethodInfo? exact = type.GetMethod(name, flags, binder: null, parameters, modifiers: null);
		if (exact != null)
		{
			return exact;
		}

		MethodInfo[] candidates = type.GetMethods(flags | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
			.Where(method => method.Name == name && method.GetParameters().Length == parameters.Length)
			.ToArray();
		if (candidates.Length == 1)
		{
			return candidates[0];
		}

		throw new InvalidOperationException($"Could not find required method {type.FullName}.{name}.");
	}

}
