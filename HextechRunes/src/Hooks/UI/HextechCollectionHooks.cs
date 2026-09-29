using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Unlocks;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static partial class HextechCollectionHooks
{
	private const float GenericToCharacterPoolSpacing = 96f;

	// 以下均为原版 NRelicCollectionCategory / NRelicCollection 的私有成员(0.107.1 / 0.110.0 / 0.111.0 同名同签名)。
	// 缺失时不逐项告警,由 CollectionHooksAvailable 按"整体停用/退化为平铺"汇总一次。
	private static readonly FieldInfo? HeaderLabelField = TryGetField(
		typeof(NRelicCollectionCategory),
		"_headerLabel",
		BindingFlags.Instance | BindingFlags.NonPublic,
		warnIfMissing: false);

	private static readonly FieldInfo? SubCategoriesField = TryGetField(
		typeof(NRelicCollectionCategory),
		"_subCategories",
		BindingFlags.Instance | BindingFlags.NonPublic,
		warnIfMissing: false);

	private static readonly FieldInfo? RelicsContainerField = TryGetField(
		typeof(NRelicCollectionCategory),
		"_relicsContainer",
		BindingFlags.Instance | BindingFlags.NonPublic,
		warnIfMissing: false);

	private static readonly MethodInfo? CreateForSubcategoryMethod = TryGetMethod(
		typeof(NRelicCollectionCategory),
		"CreateForSubcategory",
		BindingFlags.Instance | BindingFlags.NonPublic,
		warnIfMissing: false);

	private static readonly MethodInfo? LoadSubcategoryMethod = TryGetMethod(
		typeof(NRelicCollectionCategory),
		"LoadSubcategory",
		BindingFlags.Instance | BindingFlags.NonPublic,
		warnIfMissing: false,
		typeof(NRelicCollection),
		typeof(LocString),
		typeof(IEnumerable<RelicModel>),
		typeof(HashSet<RelicModel>),
		typeof(HashSet<RelicModel>));

	private static readonly MethodInfo? LoadRelicsMethod = TryGetMethod(
		typeof(NRelicCollectionCategory),
		"LoadRelics",
		BindingFlags.Instance | BindingFlags.Public,
		warnIfMissing: false,
		typeof(RelicRarity),
		typeof(NRelicCollection),
		typeof(LocString),
		typeof(HashSet<RelicModel>),
		typeof(UnlockState),
		typeof(HashSet<RelicModel>));

	private static readonly MethodInfo? CollectionClearRelicsMethod = TryGetMethod(
		typeof(NRelicCollection),
		"ClearRelics",
		BindingFlags.Instance | BindingFlags.NonPublic,
		warnIfMissing: false);

	private static readonly MethodInfo? CollectionLoadRelicsMethod = TryGetMethod(
		typeof(NRelicCollection),
		"LoadRelics",
		BindingFlags.Instance | BindingFlags.NonPublic,
		warnIfMissing: false);

	private static string? _starterHeaderTemplate;

	private static bool _loggedFlatFallback;

	private static bool _loggedMissingFallbackContainer;

	/// <summary>图鉴分类依赖原版 NRelicCollectionCategory 的一组私有成员;主目标缺失整体停用,子分类成员缺失退化为平铺网格。</summary>
	private static bool CollectionHooksAvailable
	{
		get
		{
			if (LoadRelicsMethod == null)
			{
				HextechLog.Warn("Mayhem", $"Relic collection hooks disabled: missing NRelicCollectionCategory.LoadRelics.");
				return false;
			}

			List<string> missingSubcategoryDependencies = GetMissingSubcategoryDependencies().ToList();
			if (missingSubcategoryDependencies.Count > 0)
			{
				if (RelicsContainerField == null)
				{
					HextechLog.Warn("Mayhem", $"Relic collection hooks disabled: missing {string.Join(", ", missingSubcategoryDependencies.Append("NRelicCollectionCategory._relicsContainer"))}.");
					return false;
				}

				HextechLog.Warn("Mayhem", $"Relic collection subcategory hooks unavailable: missing {string.Join(", ", missingSubcategoryDependencies)}; using flat starter-grid fallback.");
			}

			return true;
		}
	}

	public static void RefreshOpenRelicCollections()
	{
		if (CollectionClearRelicsMethod == null || CollectionLoadRelicsMethod == null)
		{
			return;
		}

		Node? root = NGame.Instance?.GetTree()?.Root;
		if (root == null || !GodotObject.IsInstanceValid(root))
		{
			return;
		}

		int refreshed = 0;
		foreach (NRelicCollection collection in EnumerateNodes<NRelicCollection>(root))
		{
			if (!GodotObject.IsInstanceValid(collection) || !collection.IsInsideTree())
			{
				continue;
			}

			try
			{
				CollectionClearRelicsMethod.Invoke(collection, null);
				CollectionLoadRelicsMethod.Invoke(collection, null);
				refreshed++;
			}
			catch (Exception ex)
			{
				HextechLog.Warn("RuneConfig", $"Failed to refresh relic collection after config save: {ex.GetType().Name}: {ex.Message}");
			}
		}

		if (refreshed > 0)
		{
			HextechLog.Info("RuneConfig", $"Refreshed {refreshed} relic collection screen(s) after config save.");
		}
	}

	private static IEnumerable<TNode> EnumerateNodes<TNode>(Node node)
		where TNode : Node
	{
		if (node is TNode match)
		{
			yield return match;
		}

		foreach (Node child in node.GetChildren())
		{
			foreach (TNode descendant in EnumerateNodes<TNode>(child))
			{
				yield return descendant;
			}
		}
	}

	[HarmonyPatch]
	[HextechPatch("ui.relic-collection", "遗物图鉴分类", Optional = true)]
	private static class LoadRelicsPatch
	{
		[HarmonyPrepare]
		private static bool Prepare() => CollectionHooksAvailable;

		[HarmonyTargetMethod]
		private static MethodBase TargetMethod() => LoadRelicsMethod!;

		[HarmonyPostfix]
		private static void Postfix(
			NRelicCollectionCategory __instance,
			RelicRarity relicRarity,
			NRelicCollection collection,
			LocString header,
			HashSet<RelicModel> seenRelics,
			UnlockState unlockState,
			HashSet<RelicModel> allUnlockedRelics)
		{
			if (relicRarity != RelicRarity.Starter)
			{
				return;
			}

			_starterHeaderTemplate ??= header.GetRawText();
			if (!CanUseSubcategoryHooks())
			{
				AddFlatFallbackRelics(__instance, collection);
				return;
			}

			AddHextechSubcategory(__instance, collection, seenRelics, allUnlockedRelics);
			AddForgeSubcategory(__instance, collection, seenRelics, allUnlockedRelics);
		}
	}
}
