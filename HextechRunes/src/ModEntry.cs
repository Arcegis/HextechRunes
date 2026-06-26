using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
	private const string HarmonyId = "Natsuki.HextechRunes";

	private static readonly object InitializeLock = new();
	private static Harmony? _harmony;
	private static bool _initialized;

	public static void Initialize()
	{
		lock (InitializeLock)
		{
			if (_initialized)
			{
				HextechLog.Info($"[{ModInfo.Id}] Initialization already completed; skipping duplicate call.");
				return;
			}

			HextechModelBootstrap.Install();
			HextechRuneConfiguration.Initialize();
			HextechTelemetry.Initialize();
			Harmony harmony = _harmony ??= new Harmony(HarmonyId);
			TryInstallOptionalHookGroup("model id serialization warning compatibility", () => HextechModelIdSerializationWarningHooks.Install(harmony));
			HextechMultiplayerCompatibilityHooks.Install(harmony);
			HextechMobileModelRegistrationHooks.Install(harmony);
			ThoughtOverwriteKeywordPersistenceHooks.Install(harmony);
			HextechSelfUpgradeCardStore.Install(harmony);
			HextechCustomRunModifierHooks.Install(harmony);
			HextechRunLifecycleHooks.Install(harmony);
			HextechCombatHooks.Install(harmony);
			HextechEnemyPowerScalingHooks.Install(harmony);
			TryInstallOptionalHookGroup("artifact encounter compatibility", () => HextechArtifactCompatibilityHooks.Install(harmony));
			TryInstallOptionalHookGroup("encounter compatibility", () => HextechEncounterCompatibilityHooks.Install(harmony));
			HextechUpdateChecker.Install(harmony);
			TryInstallOptionalHookGroup("inspect relic screen", () => HextechInspectHooks.Install(harmony));
			AssetHooks.Install(harmony);
			TryInstallOptionalHookGroup("relic collection", () => CollectionHooks.Install(harmony));
				TryInstallOptionalHookGroup("shop random forge", () => HextechShopForgeHooks.Install(harmony));
				TryInstallOptionalHookGroup("forge stacking", () => HextechForgeStackingHooks.Install(harmony));
				TryInstallOptionalHookGroup("enemy tezcataras mercy wax relics", () => HextechEnemyTezcatarasMercyHooks.Install(harmony));
				TryInstallOptionalHookGroup("enemy hex top bar hover", () => HextechEnemyUi.Install(harmony));
				TryInstallOptionalHookGroup("relic UI safety", () => HextechUiSafetyHooks.Install(harmony));
			TryInstallOptionalHookGroup("player stats hover", () => HextechPlayerStatsHoverHooks.Install(harmony));
			TryInstallOptionalHookGroup("rune configuration menu", () => HextechRuneConfigMenuHooks.Install(harmony));
			TryInstallOptionalHookGroup("reward serialization safety", () => HextechRewardSafetyHooks.Install(harmony));
			TryInstallOptionalHookGroup("relic visibility toggle", () => HextechRelicVisibilityHooks.Install(harmony));
			TryInstallOptionalHookGroup("hand of baron aura visual", () => HextechBaronAuraHooks.Install(harmony));
			TryInstallOptionalHookGroup("burn health bar prediction", () => HextechBurnHealthBarHooks.Install(harmony));
			TryInstallOptionalHookGroup("game over score line compatibility", () => HextechGameOverCompatibilityHooks.Install(harmony));
			_initialized = true;
			// 加载确认行保持始终输出（headless 验证与用户排障都依赖它），不走 verbose 门控。
			Log.Info($"[{ModInfo.Id}] Loaded for Slay the Spire 2 {ModInfo.TargetGameVersion}.");
		}
	}

	internal static HextechMayhemModifier EnsureMayhemModifier(RunState runState)
	{
		return HextechRunLifecycleHooks.EnsureMayhemModifier(runState);
	}

	internal static Task HandleHextechActStarted(HextechMayhemModifier modifier)
	{
		return HextechRunLifecycleHooks.HandleHextechActStarted(modifier);
	}

	private static void TryInstallOptionalHookGroup(string label, Action install)
	{
		try
		{
			install();
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Optional hook group skipped: {label}: {ex.GetType().Name}: {ex.Message}");
		}
	}
}
