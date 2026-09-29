using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using HextechRunes;

namespace HextechRunes.Tests;

// Hooks/Patching 审查整理的回归测试与测试专用补丁工具（原先挂在生产代码 HextechPatcher 上）。
internal static partial class Program
{
	private const BindingFlags NestedPatchFlags = BindingFlags.Public | BindingFlags.NonPublic;

	private static TestCase[] ReviewHooksTestCases() =>
	[
		new(nameof(HarmonyPassesPostfixStateByReferenceToFinalizer), HarmonyPassesPostfixStateByReferenceToFinalizer),
		new(nameof(ScopedPatchFinalizersRestoreCallerContextAfterSynchronousFailure), ScopedPatchFinalizersRestoreCallerContextAfterSynchronousFailure)
	];

	/// <summary>只应用 <paramref name="outerType"/> 里声明的嵌套补丁类；给了名字就只应用点名的那几个。</summary>
	private static void ApplyNestedPatches(Harmony harmony, Type outerType, params string[] nestedNames)
	{
		foreach (Type nested in outerType.GetNestedTypes(NestedPatchFlags))
		{
			if (nestedNames.Length > 0 && !nestedNames.Contains(nested.Name, StringComparer.Ordinal))
			{
				continue;
			}

			if (HarmonyMethodExtensions.GetFromType(nested).Any())
			{
				harmony.CreateClassProcessor(nested).Patch();
			}
		}
	}

	/// <summary>按外层类型 + 嵌套补丁类名定位补丁方法。</summary>
	private static MethodInfo? FindPatchMethod(Type outerType, string nestedName, string methodName)
	{
		return outerType.GetNestedType(nestedName, NestedPatchFlags)
			?.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}

	/// <summary>
	/// 解析嵌套补丁类 <c>[HarmonyPatch]</c> 属性声明的真实目标（不执行 Prepare/TargetMethod，测试进程里它们可能触碰 Godot）。
	/// </summary>
	private static MethodBase ResolveDeclaredPatchTarget(Type outerType, string nestedName)
	{
		Type patchType = outerType.GetNestedType(nestedName, NestedPatchFlags)
			?? throw new MissingMemberException(outerType.FullName, nestedName);
		HarmonyMethod merged = HarmonyMethod.Merge(HarmonyMethodExtensions.GetFromType(patchType));
		MethodBase? target = merged.methodType switch
		{
			MethodType.Getter => AccessTools.PropertyGetter(merged.declaringType, merged.methodName),
			MethodType.Setter => AccessTools.PropertySetter(merged.declaringType, merged.methodName),
			_ => AccessTools.Method(merged.declaringType, merged.methodName, merged.argumentTypes)
		};
		return target ?? throw new MissingMethodException(merged.declaringType?.FullName, merged.methodName);
	}

	private static class FinalizerStateProbe
	{
		internal static bool? LastFinalizerState;

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static int Run(bool fail)
		{
			if (fail)
			{
				throw new InvalidOperationException("probe failure");
			}

			return 1;
		}

		internal static void Prefix(out bool __state)
		{
			__state = true;
		}

		internal static void Postfix(ref bool __state)
		{
			__state = false;
		}

		internal static Exception? Finalizer(bool __state, Exception? __exception)
		{
			LastFinalizerState = __state;
			return null;
		}
	}

	/// <summary>
	/// AsyncLocal 作用域补丁依赖 Harmony 把 Postfix 里对 ref __state 的清零传给 Finalizer：
	/// 正常路径 Finalizer 看到已清零、不重复出栈；原方法同步抛异常时 Postfix 不执行，Finalizer 看到入栈标记。
	/// </summary>
	private static void HarmonyPassesPostfixStateByReferenceToFinalizer()
	{
		Harmony harmony = new("Natsuki.HextechRunes.Tests.FinalizerStateProbe");
		MethodInfo target = AccessTools.Method(typeof(FinalizerStateProbe), nameof(FinalizerStateProbe.Run));
		try
		{
			harmony.Patch(
				target,
				prefix: new HarmonyMethod(typeof(FinalizerStateProbe), nameof(FinalizerStateProbe.Prefix)),
				postfix: new HarmonyMethod(typeof(FinalizerStateProbe), nameof(FinalizerStateProbe.Postfix)),
				finalizer: new HarmonyMethod(typeof(FinalizerStateProbe), nameof(FinalizerStateProbe.Finalizer)));

			FinalizerStateProbe.Run(fail: false);
			Equal<bool?>(false, FinalizerStateProbe.LastFinalizerState, "finalizer sees the state cleared by the postfix");
			FinalizerStateProbe.Run(fail: true);
			Equal<bool?>(true, FinalizerStateProbe.LastFinalizerState, "finalizer sees the prefix state when the original throws");
		}
		finally
		{
			harmony.UnpatchAll(harmony.Id);
		}
	}

	private static int ReadDoubleVisionSuppressionDepth()
	{
		AsyncLocal<int> depth = (AsyncLocal<int>)(typeof(DoubleVisionRune)
			.GetField("CommandDuplicationSuppressionDepth", BindingFlags.NonPublic | BindingFlags.Static)
			?.GetValue(null)
			?? throw new MissingFieldException(nameof(DoubleVisionRune), "CommandDuplicationSuppressionDepth"));
		return depth.Value;
	}

	private static void ScopedPatchFinalizersRestoreCallerContextAfterSynchronousFailure()
	{
		InvalidOperationException failure = new("synchronous failure inside the patched command");

		// 复视奖励命令抑制：原命令同步抛异常后，调用方不能带着抑制深度继续结算后续奖励。
		MethodInfo rewardPrefix = FindPatchMethod(typeof(HextechRewardSafetyHooks), "RewardSelectUnsynchronizedPatch", "Prefix")
			?? throw new MissingMethodException("RewardSelectUnsynchronizedPatch.Prefix");
		MethodInfo rewardPostfix = FindPatchMethod(typeof(HextechRewardSafetyHooks), "RewardSelectUnsynchronizedPatch", "Postfix")
			?? throw new MissingMethodException("RewardSelectUnsynchronizedPatch.Postfix");
		MethodInfo rewardFinalizer = FindPatchMethod(typeof(HextechRewardSafetyHooks), "RewardSelectUnsynchronizedPatch", "Finalizer")
			?? throw new MissingMethodException("RewardSelectUnsynchronizedPatch.Finalizer");
		Equal(0, ReadDoubleVisionSuppressionDepth(), "clean caller context before the reward command");
		object?[] rewardState = [null];
		rewardPrefix.Invoke(null, rewardState);
		Equal(1, ReadDoubleVisionSuppressionDepth(), "prefix enters reward command suppression");
		object? rethrown = rewardFinalizer.Invoke(null, [rewardState[0], failure]);
		Expect(ReferenceEquals(failure, rethrown), "finalizer must rethrow the original exception");
		Equal(0, ReadDoubleVisionSuppressionDepth(), "finalizer restores the caller context after a synchronous failure");

		rewardPrefix.Invoke(null, rewardState);
		rewardPostfix.Invoke(null, rewardState);
		Equal(0, ReadDoubleVisionSuppressionDepth(), "postfix restores the caller context on the normal path");
		Equal<object?>(null, rewardState[0], "postfix clears the state it already handled");
		rewardFinalizer.Invoke(null, [rewardState[0], failure]);
		Equal(0, ReadDoubleVisionSuppressionDepth(), "a later failure must not restore twice");

#if STS2_110_OR_NEWER
		// 疫情响应守卫：深度守卫不是幂等的，重复出栈会弹掉外层作用域。
		MethodInfo outbreakPrefix = FindPatchMethod(typeof(HextechCombatHooks), "OutbreakPatch", "Prefix")
			?? throw new MissingMethodException("OutbreakPatch.Prefix");
		MethodInfo outbreakPostfix = FindPatchMethod(typeof(HextechCombatHooks), "OutbreakPatch", "Postfix")
			?? throw new MissingMethodException("OutbreakPatch.Postfix");
		MethodInfo outbreakFinalizer = FindPatchMethod(typeof(HextechCombatHooks), "OutbreakPatch", "Finalizer")
			?? throw new MissingMethodException("OutbreakPatch.Finalizer");
		Expect(!HextechCombatHooks.IsResolvingOutbreakPowerPoisonResponse, "clean caller context before Outbreak");
		object?[] outbreakState = [null];
		outbreakPrefix.Invoke(null, outbreakState);
		Expect(HextechCombatHooks.IsResolvingOutbreakPowerPoisonResponse, "Outbreak prefix enters the response guard");
		outbreakFinalizer.Invoke(null, [outbreakState[0], failure]);
		Expect(!HextechCombatHooks.IsResolvingOutbreakPowerPoisonResponse, "Outbreak finalizer exits the guard after a synchronous failure");

		Task outer = HextechCombatHooks.RunWithOutbreakPowerPoisonResponseGuard(() =>
		{
			object?[] nested = [null];
			outbreakPrefix.Invoke(null, nested);
			object?[] postfixArgs = [nested[0], Task.CompletedTask];
			outbreakPostfix.Invoke(null, postfixArgs);
			outbreakFinalizer.Invoke(null, [postfixArgs[0], failure]);
			Expect(HextechCombatHooks.IsResolvingOutbreakPowerPoisonResponse, "a failure after the postfix must not pop the enclosing guard");
			return Task.CompletedTask;
		});
		outer.GetAwaiter().GetResult();
		Expect(!HextechCombatHooks.IsResolvingOutbreakPowerPoisonResponse, "caller context is clean after the enclosing guard");
#endif
	}
}
