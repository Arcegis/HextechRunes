using HextechRunes;

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
	private const string LogTag = "Registration";

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
		(typeof(OtterAndFriendsRune), HextechRarityTier.Prismatic, "MULTIPLAYER"),
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
		.. AbyssalContractCatalog.ChoiceRelicTypes
	];

	/// <summary>
	/// 按表顺序逐条注册，每条独立容错。注册不是事务:本体的注册表没有回滚口子,一条失败不能把前面已入池的内容撤回,
	/// 所以失败只记 Warn 并计数,其余条目照常注册。表顺序决定池内顺序,类别之间的先后保持不变。返回失败条数。
	/// 补丁由入口无条件应用(每个补丁都以"玩家持有对应符文"为前提,内容缺席时只是空转)。
	/// </summary>
	internal static int RegisterAll()
	{
		int failed = 0;

		foreach (Type carrier in SavedPropertyCarriers)
		{
			failed += Register("SavedProperty carrier", carrier, () => HextechRunesApi.RegisterSavedPropertyCarrier(carrier));
		}

		foreach ((Type enchantment, string iconFile) in EnchantmentIcons)
		{
			failed += Register("enchantment icon", enchantment, () => HextechRunesApi.RegisterEnchantmentIcon(enchantment, $"res://{ModInfo.Id}/images/enchantments/{iconFile}"));
		}

		foreach (Type relic in EventRelics)
		{
			failed += Register("event relic", relic, () => HextechRunesApi.RegisterEventRelic(relic, ModInfo.Id));
		}

		foreach ((Type forge, HextechRarityTier rarity) in Forges)
		{
			failed += Register("forge", forge, () => HextechRunesApi.RegisterForge(forge, rarity, ModInfo.Id));
		}

		foreach ((Type rune, HextechRarityTier rarity, string tagKey) in PlayerRunes)
		{
			failed += Register("player rune", rune, () => HextechRunesApi.RegisterPlayerRune(rune, rarity, tagKey: tagKey, assetModId: ModInfo.Id));
		}

		// 信徒的锻造器售价修正走本体公开登记点;本体过旧(没有该方法)时只记失败,不影响其他内容。
		failed += Register("forge shop price modifier", typeof(BelieverRune), RegisterBelieverForgePriceModifier);

		SponsorLog.Info(LogTag, $"Registered sponsor-pack content ({PlayerRunes.Length} runes, {Forges.Length} forges, {EventRelics.Length} event relics); {failed} failed.");
		return failed;
	}

	private static int Register(string kind, Type type, Action register)
	{
		try
		{
			register();
			return 0;
		}
		catch (Exception ex)
		{
			SponsorLog.Warn(LogTag, $"Failed to register {kind} {type.Name}: {ex.GetType().Name}: {ex.Message}");
			return 1;
		}
	}

	// 单独成方法:本体缺少 RegisterForgeShopPriceModifier 时,MissingMethodException 在调用本方法时抛出,由 Register 接住。
	private static void RegisterBelieverForgePriceModifier()
	{
		HextechRunesApi.RegisterForgeShopPriceModifier(BelieverRune.ApplyForgePriceDeltas);
	}
}
