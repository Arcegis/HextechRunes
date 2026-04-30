using System.Collections.Generic;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Unlocks;
using MonoMod.RuntimeDetour;

namespace HextechRunes;

internal static class CollectionHooks
{
	private readonly record struct SubcategoryHeaderText(string ZhHeader, string ZhBody, string EnHeader, string EnBody);

	private const float GenericToCharacterPoolSpacing = 96f;

	private const string StarterHeaderZh = "初始：";

	private const string StarterHeaderZhBody = "角色们开始游戏时自身携带的遗物。";

	private const string HextechHeaderZh = "海克斯：";

	private const string HextechHeaderZhBody = "来自海克斯符文池的自定义遗物。";

	private const string ForgeHeaderZh = "属性锻造器：";

	private const string ForgeHeaderZhBody = "来自属性锻造系统的自定义遗物。";

	private const string StarterHeaderEn = "Starter:";

	private const string StarterHeaderEnBody = "Relics that characters start the game with.";

	private const string HextechHeaderEn = "Hextech:";

	private const string HextechHeaderEnBody = "Custom relics from the Hextech rune pool.";

	private const string ForgeHeaderEn = "Stat Forgers:";

	private const string ForgeHeaderEnBody = "Custom relics from the stat forging system.";

	private static readonly IReadOnlyDictionary<string, SubcategoryHeaderText> CharacterHeaderTexts = new Dictionary<string, SubcategoryHeaderText>
	{
		["CHARACTER.IRONCLAD"] = new("战士海克斯：", "仅战士可抽取的海克斯符文。", "Ironclad Hexes:", "Hextech runes only available to Ironclad."),
		["CHARACTER.SILENT"] = new("猎人海克斯：", "仅猎人可抽取的海克斯符文。", "Silent Hexes:", "Hextech runes only available to Silent."),
		["CHARACTER.REGENT"] = new("储君海克斯：", "仅储君可抽取的海克斯符文。", "Regent Hexes:", "Hextech runes only available to Regent."),
		["CHARACTER.DEFECT"] = new("故障机器人海克斯：", "仅故障机器人可抽取的海克斯符文。", "Defect Hexes:", "Hextech runes only available to Defect."),
		["CHARACTER.NECROBINDER"] = new("亡灵契约师海克斯：", "仅亡灵契约师可抽取的海克斯符文。", "Necrobinder Hexes:", "Hextech runes only available to Necrobinder.")
	};

	private static readonly FieldInfo HeaderLabelField = RequireField(typeof(NRelicCollectionCategory), "_headerLabel");

	private static readonly FieldInfo SubCategoriesField = RequireField(typeof(NRelicCollectionCategory), "_subCategories");

	private static readonly MethodInfo CreateForSubcategoryMethod = RequireMethod(typeof(NRelicCollectionCategory), "CreateForSubcategory", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly MethodInfo LoadSubcategoryMethod = RequireMethod(
		typeof(NRelicCollectionCategory),
		"LoadSubcategory",
		BindingFlags.Instance | BindingFlags.NonPublic,
		typeof(NRelicCollection),
		typeof(LocString),
		typeof(IEnumerable<RelicModel>),
		typeof(HashSet<RelicModel>),
		typeof(HashSet<RelicModel>));

	private static Hook? _loadRelicsHook;

	private static string? _starterHeaderTemplate;

	private delegate void OrigLoadRelics(
		NRelicCollectionCategory self,
		RelicRarity relicRarity,
		NRelicCollection collection,
		LocString header,
		HashSet<RelicModel> seenRelics,
		UnlockState unlockState,
		HashSet<RelicModel> allUnlockedRelics);

	public static void Install()
	{
		_loadRelicsHook = new Hook(
			RequireMethod(
				typeof(NRelicCollectionCategory),
				"LoadRelics",
				BindingFlags.Instance | BindingFlags.Public,
				typeof(RelicRarity),
				typeof(NRelicCollection),
				typeof(LocString),
				typeof(HashSet<RelicModel>),
				typeof(UnlockState),
				typeof(HashSet<RelicModel>)),
			LoadRelicsDetour);
	}

	private static void LoadRelicsDetour(
		OrigLoadRelics orig,
		NRelicCollectionCategory self,
		RelicRarity relicRarity,
		NRelicCollection collection,
		LocString header,
		HashSet<RelicModel> seenRelics,
		UnlockState unlockState,
		HashSet<RelicModel> allUnlockedRelics)
	{
		orig(self, relicRarity, collection, header, seenRelics, unlockState, allUnlockedRelics);

		if (relicRarity != RelicRarity.Starter)
		{
			return;
		}

		_starterHeaderTemplate ??= header.GetRawText();
		AddHextechSubcategory(self, collection, seenRelics, allUnlockedRelics);
		AddForgeSubcategory(self, collection, seenRelics, allUnlockedRelics);
	}

	private static void AddHextechSubcategory(
		NRelicCollectionCategory self,
		NRelicCollection collection,
		HashSet<RelicModel> seenRelics,
		HashSet<RelicModel> allUnlockedRelics)
	{
		if (collection.Relics.Any(ModInfo.IsHextechRelic))
		{
			return;
		}

		IReadOnlyList<RelicModel> genericRunes = ModInfo.GetCanonicalGenericSelectableRunes();
		IReadOnlyList<ModInfo.RuneSeriesGroup> characterGroups = ModInfo.GetCharacterRuneGroups();
		HashSet<RelicModel> visibleHextechRelics = genericRunes
			.Concat(characterGroups.SelectMany(static group => group.Relics))
			.ToHashSet();
		HashSet<RelicModel> seenWithHextech = seenRelics.Concat(visibleHextechRelics).ToHashSet();
		HashSet<RelicModel> unlockedWithHextech = allUnlockedRelics.Concat(visibleHextechRelics).ToHashSet();

		NRelicCollectionCategory subCategory = CreateAndLoadSubcategory(
			self,
			collection,
			ModInfo.HextechSubcategoryKey,
			genericRunes,
			seenWithHextech,
			unlockedWithHextech);
		ApplyCustomHeaderText(
			subCategory,
			ModInfo.HextechSubcategoryKey,
			HextechHeaderZh,
			HextechHeaderZhBody,
			HextechHeaderEn,
			HextechHeaderEnBody);

		NRelicCollectionCategory? firstCharacterSubcategory = null;
		foreach (ModInfo.RuneSeriesGroup group in characterGroups)
		{
			NRelicCollectionCategory? characterSubcategory = AddCharacterRuneSubcategory(
				subCategory,
				collection,
				group,
				seenWithHextech,
				unlockedWithHextech);
			firstCharacterSubcategory ??= characterSubcategory;
		}

		if (firstCharacterSubcategory != null)
		{
			InsertSpacingBeforeCharacterPools(subCategory, firstCharacterSubcategory);
		}
	}

	private static void AddForgeSubcategory(
		NRelicCollectionCategory self,
		NRelicCollection collection,
		HashSet<RelicModel> seenRelics,
		HashSet<RelicModel> allUnlockedRelics)
	{
		if (collection.Relics.Any(ModInfo.IsHextechForgeRelic))
		{
			return;
		}

		HashSet<RelicModel> visibleForgeRelics = ModInfo.GetCanonicalForges().ToHashSet();
		HashSet<RelicModel> seenWithForges = seenRelics.Concat(visibleForgeRelics).ToHashSet();
		HashSet<RelicModel> unlockedWithForges = allUnlockedRelics.Concat(visibleForgeRelics).ToHashSet();

		NRelicCollectionCategory subCategory = CreateAndLoadSubcategory(
			self,
			collection,
			ModInfo.ForgeSubcategoryKey,
			ModInfo.GetCanonicalForges(),
			seenWithForges,
			unlockedWithForges);
		ApplyCustomHeaderText(
			subCategory,
			ModInfo.ForgeSubcategoryKey,
			ForgeHeaderZh,
			ForgeHeaderZhBody,
			ForgeHeaderEn,
			ForgeHeaderEnBody);
	}

	private static NRelicCollectionCategory? AddCharacterRuneSubcategory(
		NRelicCollectionCategory hextechCategory,
		NRelicCollection collection,
		ModInfo.RuneSeriesGroup group,
		HashSet<RelicModel> seenRelics,
		HashSet<RelicModel> allUnlockedRelics)
	{
		if (group.Relics.Count == 0)
		{
			return null;
		}

		string localizationKey = $"HEXTECH_{group.LocalizationKey}";
		NRelicCollectionCategory subCategory = CreateAndLoadSubcategory(
			hextechCategory,
			collection,
			localizationKey,
			group.Relics,
			seenRelics,
			allUnlockedRelics);
		if (CharacterHeaderTexts.TryGetValue(group.LocalizationKey, out SubcategoryHeaderText text))
		{
			ApplyCustomHeaderText(subCategory, localizationKey, text.ZhHeader, text.ZhBody, text.EnHeader, text.EnBody);
		}

		return subCategory;
	}

	private static NRelicCollectionCategory CreateAndLoadSubcategory(
		NRelicCollectionCategory parent,
		NRelicCollection collection,
		string localizationKey,
		IEnumerable<RelicModel> relics,
		HashSet<RelicModel> seenRelics,
		HashSet<RelicModel> allUnlockedRelics)
	{
		List<NRelicCollectionCategory> subCategories = GetSubCategories(parent);
		NRelicCollectionCategory subCategory = (NRelicCollectionCategory)CreateForSubcategoryMethod.Invoke(parent, null)!;
		int insertIndex = ((Control)HeaderLabelField.GetValue(parent)!).GetIndex() + subCategories.Count + 1;
		subCategories.Add(subCategory);
		parent.AddChild(subCategory);
		parent.MoveChild(subCategory, insertIndex);

		LoadSubcategoryMethod.Invoke(
			subCategory,
			[
				collection,
				new LocString("relic_collection", localizationKey),
				relics,
				seenRelics,
				allUnlockedRelics
			]);

		return subCategory;
	}

	private static void InsertSpacingBeforeCharacterPools(
		NRelicCollectionCategory hextechCategory,
		NRelicCollectionCategory firstCharacterSubcategory)
	{
		Control spacer = new()
		{
			Name = "HextechGenericToCharacterPoolSpacer",
			CustomMinimumSize = new Vector2(0f, GenericToCharacterPoolSpacing),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		hextechCategory.AddChild(spacer);
		hextechCategory.MoveChild(spacer, firstCharacterSubcategory.GetIndex());
	}

	private static void ApplyCustomHeaderText(
		NRelicCollectionCategory subCategory,
		string localizationKey,
		string zhHeader,
		string zhBody,
		string enHeader,
		string enBody)
	{
		if (HeaderLabelField.GetValue(subCategory) is not MegaRichTextLabel headerLabel)
		{
			return;
		}

		string fallback = new LocString("relic_collection", localizationKey).GetRawText();
		headerLabel.SetTextAutoSize(FormatLikeStarterHeader(_starterHeaderTemplate, fallback, zhHeader, zhBody, enHeader, enBody));
	}

	private static string FormatLikeStarterHeader(
		string? starterTemplate,
		string fallback,
		string zhHeader,
		string zhBody,
		string enHeader,
		string enBody)
	{
		if (string.IsNullOrWhiteSpace(starterTemplate))
		{
			return fallback;
		}

		string formatted = starterTemplate
			.Replace(StarterHeaderZh, zhHeader)
			.Replace(StarterHeaderZhBody, zhBody)
			.Replace(StarterHeaderEn, enHeader)
			.Replace(StarterHeaderEnBody, enBody);

		return formatted == starterTemplate ? fallback : formatted;
	}

	private static List<NRelicCollectionCategory> GetSubCategories(NRelicCollectionCategory category)
	{
		return (List<NRelicCollectionCategory>)SubCategoriesField.GetValue(category)!;
	}

	private static FieldInfo RequireField(Type type, string name)
	{
		return type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"Could not find field {type.FullName}.{name}.");
	}

	private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] parameters)
	{
		return type.GetMethod(name, flags, binder: null, parameters, modifiers: null)
			?? throw new InvalidOperationException($"Could not find method {type.FullName}.{name}.");
	}
}
