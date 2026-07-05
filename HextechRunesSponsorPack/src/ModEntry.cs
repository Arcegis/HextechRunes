using System;
using System.Linq;
using HextechRunes;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace HextechRunesSponsorPack;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
	private const string PrerequisiteAssemblyName = "HextechRunes";

	private static readonly object InitializeLock = new();
	private static bool _initialized;
	private static bool _registered;

	public static void Initialize()
	{
		lock (InitializeLock)
		{
			if (_initialized)
			{
				Log.Info($"[{ModInfo.Id}] Initialization already completed; skipping duplicate call.");
				return;
			}

			_initialized = true;

			// 前置检测按「程序集名 HextechRunes」而非 manifest 的 mod id —— 本体与二创/synergy 版都打包了同名
			// HextechRunes.dll(含 HextechRunesApi),所以两者都能识别(manifest 里已去掉对具体 mod id 的硬依赖)。
			if (IsHextechRunesAssemblyPresent())
			{
				RegisterAll();
				return;
			}

			// 前置程序集可能晚于本拓展包加载(尤其二创版:mod id 不同、按字母序可能排在本包之后)——
			// 订阅程序集加载事件,待 HextechRunes 程序集载入后再注册,与加载顺序无关。注册发生在任何 run 开始前,内容仍及时入池。
			Log.Info($"[{ModInfo.Id}] HextechRunes assembly not loaded yet; deferring registration until it loads.");
			AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
		}
	}

	private static void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
	{
		if (!string.Equals(args.LoadedAssembly.GetName().Name, PrerequisiteAssemblyName, StringComparison.Ordinal))
		{
			return;
		}

		AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
		lock (InitializeLock)
		{
			RegisterAll();
		}
	}

	private static void RegisterAll()
	{
		if (_registered)
		{
			return;
		}

		_registered = true;
		RegisterContent();
		IntegratedStrategyEventsCompatibilityHooks.Install();
		MiracleEventPortraitPatch.Install();
		MiracleEventForgePricePatch.Install();
		MiracleEventTriggerPatch.Install();
		Log.Info($"[{ModInfo.Id}] Loaded and registered HextechRunes sponsor-pack content.");
	}

	// 兼容本体与二创(synergy)版:两者都打包了程序集名为 "HextechRunes" 的 dll(暴露同样的 HextechRunesApi)。
	private static bool IsHextechRunesAssemblyPresent()
	{
		return AppDomain.CurrentDomain.GetAssemblies()
			.Any(assembly => string.Equals(assembly.GetName().Name, PrerequisiteAssemblyName, StringComparison.Ordinal));
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
		HextechRunesApi.RegisterPlayerRune<GastritisRune>(
			HextechRarityTier.Prismatic,
			tagKey: "OUTPUT",
			assetModId: ModInfo.Id);
		HextechRunesApi.RegisterPlayerRune<EnchantmentMasterRune>(
			HextechRarityTier.Prismatic,
			tagKey: "COMPREHENSIVE",
			assetModId: ModInfo.Id);
		HextechRunesApi.RegisterPlayerRune<DesperateFinaleRune>(
			HextechRarityTier.Prismatic,
			tagKey: "COMPREHENSIVE",
			assetModId: ModInfo.Id);
		// 信徒(棱彩,仅单人):IsAvailableForPlayer 内部按 !IsNetworkMultiplayerRun() 门控单人。
		HextechRunesApi.RegisterPlayerRune<BelieverRune>(
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
