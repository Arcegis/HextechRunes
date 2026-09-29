using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HextechRunes;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes.Tests;

internal static partial class Program
{
	/// <summary>
	/// 头像系数悬浮不能再用房主(Players[0])或调试接口取状态:联机客户端会看到房主的系数。
	/// </summary>
	[HextechTest]
	private static void PlayerStatsHoverUsesLocalPortraitOwner()
	{
		Type hooks = typeof(HextechPlayerStatsHoverHooks);
		MethodInfo[] methods = hooks.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
			.Concat(hooks.GetNestedTypes(BindingFlags.NonPublic)
				.SelectMany(static nested => nested.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)))
			.Where(static method => method.GetMethodBody() != null)
			.ToArray();
		MethodInfo[] calls = methods
			.SelectMany(static method => PatchProcessor.GetOriginalInstructions(method))
			.Select(static instruction => instruction.operand)
			.OfType<MethodInfo>()
			.ToArray();
		Expect(
			calls.All(static method => method.Name != nameof(RunManager.DebugOnlyGetState)),
			"portrait stat hover must not read the debug-only run state");
		Expect(
			calls.All(static method => method.Name != "get_Players"),
			"portrait stat hover must not pick a player from the run's player list");
		Expect(
			calls.Any(static method => method.DeclaringType?.Name == "LocalContext" && method.Name == "GetMe"),
			"portrait stat hover should resolve the local player like the vanilla top bar");
	}

	/// <summary>社区面板的网络按钮不能挂 async void 处理器:异常会逃逸成未观察异常。</summary>
	[HextechTest]
	private static void ConfigMenuHasNoAsyncVoidHandlers()
	{
		IEnumerable<Type> types = new[] { typeof(HextechRuneConfigMenuHooks) }
			.Concat(typeof(HextechRuneConfigMenuHooks).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic));
		MethodInfo[] asyncVoid = types
			.SelectMany(static type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			.Where(static method => method.ReturnType == typeof(void) && method.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
			.ToArray();
		Expect(
			asyncVoid.Length == 0,
			$"config menu should start async work through TaskHelper.RunSafely, found async void: {string.Join(", ", asyncVoid.Select(static method => method.DeclaringType?.Name + "." + method.Name))}");
	}

	/// <summary>血条灼烧预测与实际结算共用同一公式。</summary>
	[HextechTest]
	private static void BurnHealthBarPredictionUsesSettlementFormula()
	{
		Equal(3, HextechBurnPower.CalculateHpLoss(50, 3), "low hp: stacks dominate");
		Equal(15, HextechBurnPower.CalculateHpLoss(500, 3), "high hp: percent dominates");
		Equal(1, HextechBurnPower.CalculateHpLoss(10, 1), "minimum loss is one stack");
	}

	/// <summary>图鉴子分类标题套用原版「初始」标题的富文本骨架,只换标题与正文,各语言通用。</summary>
	[HextechTest]
	private static void CollectionHeaderFollowsStarterTemplate()
	{
		MethodInfo format = typeof(HextechCollectionHooks).GetMethod("FormatLikeStarterHeader", BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new MissingMethodException(nameof(HextechCollectionHooks), "FormatLikeStarterHeader");
		string Format(string? template, string own) => (string)format.Invoke(null, [template, own])!;

		Equal(
			"[gold][font_size=28][b]海克斯：[/b][/font_size][/gold]来自海克斯符文池的自定义遗物。",
			Format("[gold][font_size=28][b]初始：[/b][/font_size][/gold]角色们开始游戏时自身携带的遗物。", "[gold]海克斯：[/gold] 来自海克斯符文池的自定义遗物。"),
			"zhs header keeps the vanilla layout without the gap");
		Equal(
			"[gold][font_size=28][b]Hextech:[/b][/font_size][/gold] Custom relics from the Hextech rune pool.",
			Format("[gold][font_size=28][b]Starter:[/b][/font_size][/gold] Characters begin their run with these relics.", "[gold]Hextech:[/gold] Custom relics from the Hextech rune pool."),
			"eng header replaces both title and body");
		Equal(
			"[gold]Hextech:[/gold] Body",
			Format(null, "[gold]Hextech:[/gold] Body"),
			"missing starter template falls back to the loc text");
		Equal(
			"Plain text",
			Format("[gold][font_size=28][b]Starter:[/b][/font_size][/gold] Body", "Plain text"),
			"loc text without the gold title falls back unchanged");
	}

	/// <summary>跳过原版的 UI 前缀一律 Priority.Low,并且隐藏遗物开关不再在补丁安装阶段动态打补丁。</summary>
	[HextechTest]
	private static void UiSkipPrefixesUseLowPriorityAndDeclarativeTargets()
	{
		string[] skipPatchTypes =
		[
			"HextechRunes.HextechEnemyUi+HolderFocusPatch",
			"HextechRunes.HextechEnemyUi+HolderUnfocusPatch",
			"HextechRunes.HextechInspectHooks+UpdateRelicDisplayPatch",
			"HextechRunes.HextechUiSafetyHooks+NewlyAcquiredAnimationPatch",
			"HextechRunes.HextechUiSafetyHooks+MultiplayerIntentPatch",
			"HextechRunes.HextechUiSafetyHooks+CardPlayQueuePatch",
			"HextechRunes.HextechRelicVisibilityHooks+RelicHolderDoFlashPatch",
			"HextechRunes.HextechBurnHealthBarHooks+RefreshForegroundPatch"
		];
		foreach (string typeName in skipPatchTypes)
		{
			Type type = typeof(ModEntry).Assembly.GetType(typeName)
				?? throw new TypeLoadException(typeName);
			MethodInfo prefix = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
				.Single(static method => method.GetCustomAttribute<HarmonyPrefix>() != null);
			Equal(typeof(bool), prefix.ReturnType, $"{typeName} prefix can skip the original");
			Equal(Priority.Low, prefix.GetCustomAttribute<HarmonyPriority>()?.info.priority ?? -1, $"{typeName} prefix priority");
		}

		Type visibility = typeof(HextechRelicVisibilityHooks);
		Expect(
			visibility.GetNestedTypes(BindingFlags.NonPublic)
				.All(static nested => nested.GetMethod("Apply", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, [typeof(Harmony)]) == null),
			"relic visibility patches should be declared per target instead of applied dynamically");
	}
}
