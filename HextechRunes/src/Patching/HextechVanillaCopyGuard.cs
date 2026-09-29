using System.Security.Cryptography;
using System.Text;

namespace HextechRunes;

/// <summary>
/// 原版拷贝守卫:凡本模组用 <c>bool</c> 前缀可能跳过原方法的目标,都可能复制了一段原版逻辑。
/// 游戏更新后这些拷贝会静默失真,因此启动时比对目标方法 IL 的 SHA1 与冻结表,漂移即告警。
/// </summary>
/// <remarks>
/// 目标清单来自 0.111.0 的 <c>HEXTECH_DUMP_PATCHES</c> 补丁表导出;各维护版本的行(含异步方法的
/// <c>MoveNext</c>)由测试 <c>VanillaCopyGuardFreezesEntriesAndAsyncBodies</c> 在对应 sts2.dll 上
/// 用 <c>HEXTECH_WRITE_PATCH_MANIFEST=1</c> 补齐,漂移行只报告不刷新。以嵌入资源
/// <c>vanilla_copy_guard.txt</c> 随各变体打包;没有表的变体跳过校验。
/// 行格式:<c>Namespace.Type::Method(ParamType,...)=sha1</c>。
/// </remarks>
internal static class HextechVanillaCopyGuard
{
	private const string ResourceName = "vanilla_copy_guard.txt";

	internal static string DescribeTarget(MethodBase method)
	{
		string parameters = string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.FullName ?? parameter.ParameterType.Name));
		return $"{method.DeclaringType?.FullName}::{method.Name}({parameters})";
	}

	internal static string? ComputeIlHash(MethodBase method)
	{
		byte[]? il = method.GetMethodBody()?.GetILAsByteArray();
		return il == null ? null : Convert.ToHexString(SHA1.HashData(il)).ToLowerInvariant();
	}

	// 异步方法本体只是启动状态机的桩，原版逻辑改动几乎都落在编译器生成的 MoveNext 里；
	// 只哈希入口会让"跳过原方法并复制其逻辑"的前缀在游戏更新后静默失真。
	internal static IEnumerable<MethodBase> WithAsyncBody(MethodBase method)
	{
		yield return method;
		if (TryGetAsyncMoveNext(method) is MethodInfo moveNext)
		{
			yield return moveNext;
		}
	}

	internal static MethodInfo? TryGetAsyncMoveNext(MethodBase method)
	{
		Type? stateMachine = method.GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>()?.StateMachineType;
		return stateMachine?.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	}

	/// <summary>本模组挂了可跳过原方法的前缀的所有目标。</summary>
	internal static IEnumerable<MethodBase> EnumerateSkipCapableTargets(string ownerId)
	{
		foreach (MethodBase method in Harmony.GetAllPatchedMethods())
		{
			Patches? info = Harmony.GetPatchInfo(method);
			if (info != null && info.Prefixes.Any(patch => patch.owner == ownerId && patch.PatchMethod.ReturnType == typeof(bool)))
			{
				yield return method;
			}
		}
	}

	internal static IReadOnlyDictionary<string, string> LoadExpectedHashes()
	{
		Dictionary<string, string> expected = new(StringComparer.Ordinal);
		using Stream? stream = typeof(HextechVanillaCopyGuard).Assembly.GetManifestResourceStream(ResourceName);
		if (stream == null)
		{
			return expected;
		}

		using StreamReader reader = new(stream, Encoding.UTF8);
		while (reader.ReadLine() is string line)
		{
			string trimmed = line.Trim();
			int separator = trimmed.LastIndexOf('=');
			if (trimmed.Length == 0 || trimmed.StartsWith('#') || separator <= 0)
			{
				continue;
			}

			expected[trimmed[..separator]] = trimmed[(separator + 1)..];
		}

		return expected;
	}

	internal static void Verify(string ownerId)
	{
		try
		{
			IReadOnlyDictionary<string, string> expected = LoadExpectedHashes();
			if (expected.Count == 0)
			{
				HextechLog.Info("VanillaCopyGuard", $"No frozen IL table for compat target {ModInfo.TargetGameVersion}; skipping.");
				return;
			}

			List<string> drifted = [];
			List<string> unregistered = [];
			foreach (MethodBase method in EnumerateSkipCapableTargets(ownerId).SelectMany(WithAsyncBody))
			{
				string key = DescribeTarget(method);
				string? actual = ComputeIlHash(method);
				if (!expected.TryGetValue(key, out string? frozen))
				{
					unregistered.Add(key);
				}
				else if (!string.Equals(frozen, actual, StringComparison.Ordinal))
				{
					drifted.Add($"{key} frozen={frozen} actual={actual ?? "<no body>"}");
				}
			}

			if (drifted.Count > 0)
			{
				HextechLog.Warn("VanillaCopyGuard", $"DRIFT: {drifted.Count} patched method(s) changed IL since the table was frozen; review the prefixes that replace vanilla logic:\n  {string.Join("\n  ", drifted)}");
			}

			if (unregistered.Count > 0)
			{
				HextechLog.Info("VanillaCopyGuard", $"{unregistered.Count} skip-capable target(s) not in the frozen table:\n  {string.Join("\n  ", unregistered)}");
			}

			if (drifted.Count == 0)
			{
				HextechLog.Info("VanillaCopyGuard", $"{expected.Count} frozen target(s) verified.");
			}
		}
		catch (Exception ex)
		{
			HextechLog.Warn("VanillaCopyGuard", $"Verification failed: {ex.GetType().Name}: {ex.Message}");
		}
	}
}
