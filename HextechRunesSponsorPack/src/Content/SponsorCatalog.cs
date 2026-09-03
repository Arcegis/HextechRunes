using HextechRunes;
using MegaCrit.Sts2.Core.Logging;

namespace HextechRunesSponsorPack;

/// <summary>
/// 拓展包的内容清单:符文 / 锻造器 / 事件遗物 / SavedProperty 载体 / 附魔图标各一张只读表,
/// <see cref="RegisterAll"/> 按表逐条调 <see cref="HextechRunesApi"/>。
/// </summary>
/// <remarks>
/// 表内顺序即注册顺序,决定 ModelDb 的发现顺序与池内顺序,是 append-only 清单:新增条目追加到对应表末尾,不重排既有条目。
/// </remarks>
internal static class SponsorCatalog
{
	// 迁移壳 SponsorCompositeEnchantment 仍带 [SavedProperty],载体注册保留;它不再有图标(不注册、不进随机附魔池)。
	private static readonly Type[] SavedPropertyCarriers =
	[
		typeof(Evolution),
		typeof(EntropyIncrease),
		typeof(EntropyDecrease),
		typeof(SponsorCompositeEnchantment)
	];

	private static readonly (Type Enchantment, string IconFile)[] EnchantmentIcons =
	[
		(typeof(Evolution), "evolution.png"),
		(typeof(EntropyIncrease), "plus.png"),
		(typeof(EntropyDecrease), "minus.png")
	];

	private static readonly (Type Forge, HextechRarityTier Rarity)[] Forges =
	[
		(typeof(BasicForge), HextechRarityTier.Gold),
		(typeof(EnchantmentForge), HextechRarityTier.Gold),
		(typeof(EntropyForge), HextechRarityTier.Gold),
		(typeof(ArcaneForge), HextechRarityTier.Prismatic),
		(typeof(DollysMirrorForge), HextechRarityTier.Prismatic),
		(typeof(EvolutionForge), HextechRarityTier.Prismatic),
		(typeof(MysticForge), HextechRarityTier.Prismatic)
	];

	// 信徒(棱彩,仅单人):IsAvailableForPlayer 内部按 !IsNetworkMultiplayerRun() 门控单人。
	private static readonly (Type Rune, HextechRarityTier Rarity, string TagKey)[] PlayerRunes =
	[
		(typeof(StarlightSparkleRune), HextechRarityTier.Gold, "COMPREHENSIVE"),
		(typeof(CosplayRune), HextechRarityTier.Prismatic, "COMPREHENSIVE"),
		(typeof(OtterAndFriendsRune), HextechRarityTier.Prismatic, "COMPREHENSIVE"),
		(typeof(RegretRune), HextechRarityTier.Prismatic, "SURVIVAL"),
		(typeof(GastritisRune), HextechRarityTier.Prismatic, "OUTPUT"),
		(typeof(EnchantmentMasterRune), HextechRarityTier.Prismatic, "COMPREHENSIVE"),
		(typeof(DesperateFinaleRune), HextechRarityTier.Prismatic, "COMPREHENSIVE"),
		(typeof(AbyssalContractRune), HextechRarityTier.Prismatic, "COMPREHENSIVE"),
		(typeof(BelieverRune), HextechRarityTier.Prismatic, "COMPREHENSIVE")
	];

	private static readonly Type[] EventRelics =
	[
		typeof(GoldStarRelic),
		typeof(ArcaneCloneChoiceRelic),
		typeof(ArcaneSoulsPowerChoiceRelic),
		typeof(ArcaneRoyallyApprovedChoiceRelic),
		typeof(DollyCardChoiceRelic),
		typeof(DollyRelicChoiceRelic),
		typeof(DollyPreviousPageRelic),
		typeof(DollyNextPageRelic),
		typeof(EntropyIncreaseChoiceRelic),
		typeof(EntropyDecreaseChoiceRelic),
		typeof(WarriorContractChoiceRelic),
		typeof(HunterContractChoiceRelic),
		typeof(RegentContractChoiceRelic),
		typeof(NecrobinderContractChoiceRelic),
		typeof(AutomatonContractChoiceRelic)
	];

	internal static void RegisterAll()
	{
		foreach (Type carrier in SavedPropertyCarriers)
		{
			HextechRunesApi.RegisterSavedPropertyCarrier(carrier);
		}

		foreach ((Type enchantment, string iconFile) in EnchantmentIcons)
		{
			HextechRunesApi.RegisterEnchantmentIcon(enchantment, $"res://{ModInfo.Id}/images/enchantments/{iconFile}");
		}

		foreach ((Type forge, HextechRarityTier rarity) in Forges)
		{
			HextechRunesApi.RegisterForge(forge, rarity, ModInfo.Id);
		}

		foreach ((Type rune, HextechRarityTier rarity, string tagKey) in PlayerRunes)
		{
			HextechRunesApi.RegisterPlayerRune(rune, rarity, tagKey: tagKey, assetModId: ModInfo.Id);
		}

		Log.Info($"[{ModInfo.Id}] Registered IntegratedStrategyEvents soft-collab rune content with runtime availability gating.");

		foreach (Type relic in EventRelics)
		{
			HextechRunesApi.RegisterEventRelic(relic, ModInfo.Id);
		}
	}
}
