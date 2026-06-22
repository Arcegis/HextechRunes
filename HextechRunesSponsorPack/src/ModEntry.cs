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
			_initialized = true;
			Log.Info($"[{ModInfo.Id}] Loaded and registered HextechRunes sponsor-pack content.");
		}
	}

	private static void RegisterContent()
	{
		HextechRunesApi.RegisterEnchantmentIcon<Evolution>($"res://{ModInfo.Id}/images/enchantments/evolution.png");
		HextechRunesApi.RegisterForge<EnchantmentForge>(HextechRarityTier.Gold, ModInfo.Id);
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
		HextechRunesApi.RegisterEventRelic<GoldStarRelic>(ModInfo.Id);
	}
}
