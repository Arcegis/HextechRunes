using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using DetourHook = MonoMod.RuntimeDetour.Hook;
using CoreHook = MegaCrit.Sts2.Core.Hooks.Hook;

namespace HextechRunes;

internal static class HextechShopForgeHooks
{
	private const int RandomForgeShopInitialCost = 150;
	private const int RandomForgeShopCostIncrease = 50;
	private const float CardRemovalRandomForgeOffsetY = 60f;

	private static DetourHook? _createForNormalMerchantHook;

	private static DetourHook? _merchantRelicPurchaseHook;

	private static DetourHook? _merchantRelicRestockHook;

	private static DetourHook? _modifyMerchantPriceHook;

	private static DetourHook? _shouldRefillMerchantEntryHook;

	private static DetourHook? _merchantInventoryInitializeHook;

	private static FieldInfo? _relicEntriesField;

	private static FieldInfo? _merchantInventoryRelicContainerField;

	private static FieldInfo? _merchantInventoryCardRemovalNodeField;

	private static readonly Dictionary<ulong, Vector2> CardRemovalOriginalPositions = [];

	private delegate MerchantInventory OrigCreateForNormalMerchant(Player player);

	private delegate Task<(bool, int)> OrigMerchantRelicPurchase(MerchantRelicEntry self, MerchantInventory inventory, bool ignoreCost);

	private delegate void OrigMerchantRelicRestock(MerchantRelicEntry self, MerchantInventory inventory);

	private delegate void OrigMerchantInventoryInitialize(NMerchantInventory self, MerchantInventory inventory, MerchantDialogueSet dialogue);

	private delegate bool OrigShouldRefillMerchantEntry(IRunState runState, MerchantEntry entry, Player player);

	private delegate decimal OrigModifyMerchantPrice(IRunState runState, Player player, MerchantEntry entry, decimal result);

	public static void Install()
	{
		_relicEntriesField = RequireField(typeof(MerchantInventory), "_relicEntries");
		_merchantInventoryRelicContainerField = RequireField(typeof(NMerchantInventory), "_relicContainer");
		_merchantInventoryCardRemovalNodeField = RequireField(typeof(NMerchantInventory), "_cardRemovalNode");
		_createForNormalMerchantHook = new DetourHook(
			RequireMethod(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant), BindingFlags.Static | BindingFlags.Public, typeof(Player)),
			CreateForNormalMerchantDetour);
		_merchantRelicPurchaseHook = new DetourHook(
			RequireMethod(typeof(MerchantRelicEntry), "OnTryPurchase", BindingFlags.Instance | BindingFlags.NonPublic, typeof(MerchantInventory), typeof(bool)),
			MerchantRelicPurchaseDetour);
		_merchantRelicRestockHook = new DetourHook(
			RequireMethod(typeof(MerchantRelicEntry), "RestockAfterPurchase", BindingFlags.Instance | BindingFlags.NonPublic, typeof(MerchantInventory)),
			MerchantRelicRestockDetour);
		_modifyMerchantPriceHook = new DetourHook(
			RequireMethod(typeof(CoreHook), nameof(CoreHook.ModifyMerchantPrice), BindingFlags.Static | BindingFlags.Public, typeof(IRunState), typeof(Player), typeof(MerchantEntry), typeof(decimal)),
			ModifyMerchantPriceDetour);
		_shouldRefillMerchantEntryHook = new DetourHook(
			RequireMethod(typeof(CoreHook), nameof(CoreHook.ShouldRefillMerchantEntry), BindingFlags.Static | BindingFlags.Public, typeof(IRunState), typeof(MerchantEntry), typeof(Player)),
			ShouldRefillMerchantEntryDetour);
		_merchantInventoryInitializeHook = new DetourHook(
			RequireMethod(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize), BindingFlags.Instance | BindingFlags.Public, typeof(MerchantInventory), typeof(MerchantDialogueSet)),
			MerchantInventoryInitializeDetour);
	}

	private static MerchantInventory CreateForNormalMerchantDetour(OrigCreateForNormalMerchant orig, Player player)
	{
		MerchantInventory inventory = orig(player);
		InstallRandomForgeEntry(inventory, player);
		return inventory;
	}

	private static decimal ModifyMerchantPriceDetour(OrigModifyMerchantPrice orig, IRunState runState, Player player, MerchantEntry entry, decimal result)
	{
		if (TryGetRandomForgeShopRelic(entry, out RandomForgeShopRelic? shopRelic) && shopRelic != null)
		{
			return GetRandomForgeShopCost(shopRelic);
		}

		return orig(runState, player, entry, result);
	}

	private static bool ShouldRefillMerchantEntryDetour(OrigShouldRefillMerchantEntry orig, IRunState runState, MerchantEntry entry, Player player)
	{
		return IsRandomForgeEntry(entry) || orig(runState, entry, player);
	}

	private static void MerchantInventoryInitializeDetour(OrigMerchantInventoryInitialize orig, NMerchantInventory self, MerchantInventory inventory, MerchantDialogueSet dialogue)
	{
		EnsureRandomForgeRelicSlot(self, inventory);
		orig(self, inventory, dialogue);
		MoveCardRemovalBelowRandomForge(self, inventory);
	}

	private static Task<(bool, int)> MerchantRelicPurchaseDetour(OrigMerchantRelicPurchase orig, MerchantRelicEntry self, MerchantInventory inventory, bool ignoreCost)
	{
		if (!IsRandomForgeEntry(self))
		{
			return orig(self, inventory, ignoreCost);
		}

		return PurchaseRandomForge(self, inventory, ignoreCost);
	}

	private static void MerchantRelicRestockDetour(OrigMerchantRelicRestock orig, MerchantRelicEntry self, MerchantInventory inventory)
	{
		if (IsRandomForgeEntry(self))
		{
			return;
		}

		orig(self, inventory);
	}

	private static void InstallRandomForgeEntry(MerchantInventory inventory, Player player)
	{
		if (_relicEntriesField?.GetValue(inventory) is not List<MerchantRelicEntry> relicEntries)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Random forge shop entry skipped: relic entry list unavailable.");
			return;
		}

		if (relicEntries.Any(IsRandomForgeEntry))
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
			: RandomForgeShopInitialCost;

		if (!HextechForgeGrantHelper.TryCreateRandomForge(player, player.PlayerRng.Shops, out RelicModel? forge) || forge == null)
		{
			entry.InvokePurchaseFailed(PurchaseStatus.FailureOutOfStock);
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
		return RandomForgeShopInitialCost + (shopRelic.PurchaseCount * RandomForgeShopCostIncrease);
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

		if (_merchantInventoryRelicContainerField?.GetValue(merchantInventory) is not Control relicContainer)
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

		object? cardRemovalNode = _merchantInventoryCardRemovalNodeField?.GetValue(merchantInventory);
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

	private static FieldInfo RequireField(Type type, string name)
	{
		return type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"Could not find required field {type.FullName}.{name}.");
	}
}
