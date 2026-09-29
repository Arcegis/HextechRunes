using System.Reflection;
using System.Text;
using HarmonyLib;

namespace HextechRunesSponsorPack;

/// <summary>
/// 属性式补丁的统一应用入口。约定与本体 <c>HextechPatcher</c> 对齐(元数据自描述、逐类应用、逐条汇报、Optional 降级、
/// 失败按功能归因),但实现独立:本体那份是 internal,还带符文可用性登记与冲突自报,公开它会把大量内部类型拖进 API 面。
/// 先后关系须显式声明 Harmony 顺序约束,不依赖类型枚举顺序;声明约束由测试 SponsorPatchDeclarationsAreCompleteAndSkipPrefixesYield 守护。
/// </summary>
internal static class SponsorPatcher
{
	private const string DumpEnvVar = "HEXTECH_SPONSOR_DUMP_PATCHES";
	private const string LogTag = "Patch";

	private sealed record PatchResult(string Id, string Feature, bool Optional, bool Applied, string? Error);

	private static readonly List<PatchResult> Results = [];

	/// <summary>非 Optional 且未能应用的补丁类数量;入口据此判断初始化是否真正成功。</summary>
	internal static int RequiredFailureCount => Results.Count(static result => !result.Applied && !result.Optional);

	/// <summary>
	/// 应用 <paramref name="assembly"/> 里的补丁类:带 <c>[HarmonyPatch]</c> 的走 Harmony 类处理器;
	/// 只带 <c>[SponsorPatch]</c> 且声明 <c>static void Apply(Harmony)</c> 的是"动态目标"补丁(目标只能在运行时枚举)。
	/// </summary>
	internal static void ApplyAll(Harmony harmony, Assembly assembly)
	{
		foreach (Type type in AccessTools.GetTypesFromAssembly(assembly))
		{
			SponsorPatchAttribute? meta = type.GetCustomAttribute<SponsorPatchAttribute>();
			bool hasHarmonyAttributes = HarmonyMethodExtensions.GetFromType(type).Any();
			MethodInfo? dynamicApply = hasHarmonyAttributes || meta == null
				? null
				: type.GetMethod("Apply", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, [typeof(Harmony)]);
			if (!hasHarmonyAttributes && dynamicApply == null)
			{
				if (meta != null)
				{
					// 声明了元数据却没有任何目标:属性挂错了类。静默跳过等于补丁凭空消失,必须显形。
					Results.Add(new PatchResult(meta.Id, meta.Feature, meta.Optional, Applied: false, Error: "no [HarmonyPatch] target and no Apply(Harmony)"));
					SponsorLog.Warn(LogTag, $"Patch declared but has no target: {meta.Id} ({meta.Feature}) on {type.FullName}");
				}

				continue;
			}

			string id = meta?.Id ?? type.FullName ?? type.Name;
			string feature = meta?.Feature ?? "unspecified";
			bool optional = meta?.Optional == true;
			try
			{
				if (dynamicApply != null)
				{
					dynamicApply.Invoke(null, [harmony]);
				}
				else
				{
					List<MethodInfo>? patched = harmony.CreateClassProcessor(type).Patch();
					if ((patched == null || patched.Count == 0) && !optional)
					{
						throw new InvalidOperationException("class processor patched no methods");
					}
				}

				Results.Add(new PatchResult(id, feature, optional, Applied: true, Error: null));
			}
			catch (Exception ex)
			{
				Exception root = ex switch
				{
					HarmonyException { InnerException: { } inner } => inner,
					TargetInvocationException { InnerException: { } inner } => inner,
					_ => ex
				};
				Results.Add(new PatchResult(id, feature, optional, Applied: false, Error: $"{root.GetType().Name}: {root.Message}"));
				if (optional)
				{
					SponsorLog.Info(LogTag, $"Optional patch skipped: {id} ({feature}): {root.GetType().Name}: {root.Message}");
				}
				else
				{
					SponsorLog.Warn(LogTag, $"Patch failed: {id} ({feature}): {root.GetType().Name}: {root.Message}");
				}
			}
		}
	}

	/// <summary>启动汇总:应用/失败计数,失败项逐条列出。</summary>
	internal static void LogSummary()
	{
		int failed = Results.Count(static result => !result.Applied);
		SponsorLog.Info(LogTag, $"Applied {Results.Count - failed}/{Results.Count} patch classes.");
		foreach (PatchResult result in Results.Where(static result => !result.Applied))
		{
			SponsorLog.Info(LogTag, $"  failed {result.Id} ({result.Feature}{(result.Optional ? ", optional" : string.Empty)}): {result.Error}");
		}
	}

	/// <summary>
	/// 环境变量 <c>HEXTECH_SPONSOR_DUMP_PATCHES=&lt;path&gt;</c> 存在时把本模组的补丁表写成文本:
	/// 重构等价性验证工具,目标集合、种类、优先级与同目标执行序在改动前后必须一致。
	/// </summary>
	internal static void DumpIfRequested(Harmony harmony)
	{
		string? path = Environment.GetEnvironmentVariable(DumpEnvVar);
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}

		try
		{
			File.WriteAllText(path, SponsorPatchTable.Build(harmony.Id), Encoding.UTF8);
			SponsorLog.Info(LogTag, $"Patch table written to {path}.");
		}
		catch (Exception ex)
		{
			SponsorLog.Warn(LogTag, $"Patch table dump failed: {ex.GetType().Name}: {ex.Message}");
		}
	}
}
