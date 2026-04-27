using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using DetourHook = MonoMod.RuntimeDetour.Hook;
using CoreHook = MegaCrit.Sts2.Core.Hooks.Hook;

namespace HextechRunes;

internal static class HextechShopForgeHooks
{
	private const int RandomForgeShopCost = 250;

	private static DetourHook? _createForNormalMerchantHook;

	private static DetourHook? _merchantRelicPurchaseHook;

	private static DetourHook? _merchantRelicRestockHook;

	private static DetourHook? _modifyMerchantPriceHook;

	private static DetourHook? _shouldRefillMerchantEntryHook;

	private static FieldInfo? _relicEntriesField;

	private delegate MerchantInventory OrigCreateForNormalMerchant(Player player);

	private delegate Task<(bool, int)> OrigMerchantRelicPurchase(MerchantRelicEntry self, MerchantInventory inventory, bool ignoreCost);

	private delegate void OrigMerchantRelicRestock(MerchantRelicEntry self, MerchantInventory inventory);

	private delegate bool OrigShouldRefillMerchantEntry(IRunState runState, MerchantEntry entry, Player player);

	private delegate decimal OrigModifyMerchantPrice(IRunState runState, Player player, MerchantEntry entry, decimal result);

	public static void Install()
	{
		_relicEntriesField = RequireField(typeof(MerchantInventory), "_relicEntries");
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
	}

	private static MerchantInventory CreateForNormalMerchantDetour(OrigCreateForNormalMerchant orig, Player player)
	{
		MerchantInventory inventory = orig(player);
		InstallRandomForgeEntry(inventory, player);
		return inventory;
	}

	private static decimal ModifyMerchantPriceDetour(OrigModifyMerchantPrice orig, IRunState runState, Player player, MerchantEntry entry, decimal result)
	{
		if (IsRandomForgeEntry(entry))
		{
			return RandomForgeShopCost;
		}

		return orig(runState, player, entry, result);
	}

	private static bool ShouldRefillMerchantEntryDetour(OrigShouldRefillMerchantEntry orig, IRunState runState, MerchantEntry entry, Player player)
	{
		return IsRandomForgeEntry(entry) || orig(runState, entry, player);
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
		if (relicEntries.Count == 0)
		{
			inventory.AddRelicEntry(entry);
			return;
		}

		relicEntries[^1] = entry;
	}

	private static async Task<(bool, int)> PurchaseRandomForge(MerchantRelicEntry entry, MerchantInventory inventory, bool ignoreCost)
	{
		Player player = inventory.Player;
		if (!HextechForgeGrantHelper.TryCreateRandomForge(player, player.PlayerRng.Shops, out RelicModel? forge) || forge == null)
		{
			entry.InvokePurchaseFailed(PurchaseStatus.FailureOutOfStock);
			return (false, 0);
		}

		if (!ignoreCost)
		{
			await PlayerCmd.LoseGold(RandomForgeShopCost, player, GoldLossType.Spent);
			RunManager.Instance.RewardSynchronizer.SyncLocalGoldLost(RandomForgeShopCost);
		}

		player.RunState.CurrentMapPointHistoryEntry?
			.GetEntry(player.NetId)
			.BoughtRelics
			.Add(forge.Id);

		SaveManager.Instance.MarkRelicAsSeen(forge);
		await RelicCmd.Obtain(forge, player);
		RunManager.Instance.RewardSynchronizer.SyncLocalObtainedRelic(forge);
		return (true, ignoreCost ? 0 : RandomForgeShopCost);
	}

	private static bool IsRandomForgeEntry(MerchantEntry entry)
	{
		return entry is MerchantRelicEntry relicEntry && ModInfo.IsHextechShopRelic(relicEntry.Model);
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
