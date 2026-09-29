using MegaCrit.Sts2.Core.Modding;

namespace HextechRunes;

/// <summary>
/// 模组入口,只做编排:模型注册 → 配置/遥测 → 补丁应用 → 诊断输出。
/// 功能补丁由 <see cref="HextechPatcher"/> 按元数据统一应用；需要先后关系时显式声明 Harmony 顺序约束。
/// </summary>
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
				HextechLog.Info("Init", $"Initialization already completed; skipping duplicate call.");
				return;
			}

			// 先登记模型与 SavedProperty 载体，再安装依赖这些模型的补丁；net-id 冻结在后续启动收尾进行。
			HextechModelBootstrap.Install();
			HextechRuneConfiguration.Initialize();
			HextechTelemetry.Initialize();
			HextechIntegratedStrategyEventsCompat.Install();

			Harmony harmony = _harmony ??= new Harmony(HarmonyId);
			HextechPatcher.ApplyAll(harmony, typeof(ModEntry).Assembly);
			HextechPatcher.LogSummary();
			HextechPatcher.LogSharedPatchTargets(harmony);
			HextechVanillaCopyGuard.Verify(harmony.Id);
			HextechPatcher.DumpIfRequested(harmony);
			_initialized = true;
			HextechMultiplayerDiagnostics.LogNetworkSignature();
			// 加载确认行保持始终输出（headless 验证与用户排障都依赖它），不走 verbose 门控。
			HextechLog.Info(
				"Init", $"Loaded implementation variant for " +
				$"Slay the Spire 2 compat target {ModInfo.TargetGameVersion}.");
		}
	}

	internal static HextechMayhemModifier EnsureMayhemModifier(RunState runState)
	{
		return HextechRunLifecycleHooks.EnsureMayhemModifier(runState);
	}

}
