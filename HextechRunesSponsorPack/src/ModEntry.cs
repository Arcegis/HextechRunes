using HextechRunes;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace HextechRunesSponsorPack;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
	private static readonly object InitializeLock = new();
	private static bool _initialized;

	public static void Initialize()
	{
		lock (InitializeLock)
		{
			if (_initialized)
			{
				Log.Info($"[{ModInfo.Id}] Initialization already completed; skipping duplicate call.");
				return;
			}

			RegisterContent();
			IntegratedStrategyEventsCompatibilityHooks.Install();
			_initialized = true;
			Log.Info($"[{ModInfo.Id}] Loaded and registered HextechRunes sponsor-pack content.");
		}
	}

	private static void RegisterContent()
	{
		BuiltInRepeatableEnchantments.Initialize();
		HextechRunesApi.RegisterEnchantmentIcon<Evolution>($"res://{ModInfo.Id}/images/enchantments/evolution.png");
		HextechRunesApi.RegisterEnchantmentIcon<SponsorCompositeEnchantment>($"res://{ModInfo.Id}/images/relics/enchantmentMasterRune.png");
		HextechRunesApi.RegisterForge<BasicForge>(HextechRarityTier.Gold, ModInfo.Id);
		HextechRunesApi.RegisterForge<EnchantmentForge>(HextechRarityTier.Gold, ModInfo.Id);
		HextechRunesApi.RegisterForge<ArcaneForge>(HextechRarityTier.Prismatic, ModInfo.Id);
		HextechRunesApi.RegisterForge<EvolutionForge>(HextechRarityTier.Prismatic, ModInfo.Id);
		HextechRunesApi.RegisterForge<MysticForge>(HextechRarityTier.Prismatic, ModInfo.Id);
		HextechRunesApi.RegisterPlayerRune<StarlightSparkleRune>(
			HextechRarityTier.Gold,
			tagKey: "COMPREHENSIVE",
			assetModId: ModInfo.Id);
		HextechRunesApi.RegisterPlayerRune<CosplayRune>(
			HextechRarityTier.Prismatic,
			tagKey: "COMPREHENSIVE",
			assetModId: ModInfo.Id);
		HextechRunesApi.RegisterPlayerRune<OtterAndFriendsRune>(
			HextechRarityTier.Prismatic,
			tagKey: "COMPREHENSIVE",
			assetModId: ModInfo.Id);
		HextechRunesApi.RegisterPlayerRune<RegretRune>(
			HextechRarityTier.Prismatic,
			tagKey: "SURVIVAL",
			assetModId: ModInfo.Id);
		HextechRunesApi.RegisterPlayerRune<EnchantmentMasterRune>(
			HextechRarityTier.Prismatic,
			tagKey: "COMPREHENSIVE",
			assetModId: ModInfo.Id);
		HextechRunesApi.RegisterPlayerRune<DesperateFinaleRune>(
			HextechRarityTier.Prismatic,
			tagKey: "COMPREHENSIVE",
			assetModId: ModInfo.Id);
		Log.Info($"[{ModInfo.Id}] Registered IntegratedStrategyEvents soft-collab rune content with runtime availability gating.");

		HextechRunesApi.RegisterEventRelic<GoldStarRelic>(ModInfo.Id);
		HextechRunesApi.RegisterEventRelic<ArcaneCloneChoiceRelic>(ModInfo.Id);
		HextechRunesApi.RegisterEventRelic<ArcaneSoulsPowerChoiceRelic>(ModInfo.Id);
		HextechRunesApi.RegisterEventRelic<ArcaneRoyallyApprovedChoiceRelic>(ModInfo.Id);
	}
}
