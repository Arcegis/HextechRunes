using System.Reflection;
using HextechRunes;

namespace HextechRunes.Tests;

// 原版拷贝守卫冻结表的覆盖面检查。跳过型前缀目标的清单来自 0.111.0 的真实游戏补丁导出
// （HEXTECH_DUMP_PATCHES），其余维护版本在各自的 sts2.dll 里按同一清单解析；异步目标必须连同
// 编译器生成的 MoveNext 一起冻结。HEXTECH_WRITE_PATCH_MANIFEST=1 时只追加缺失的行，
// 已冻结却漂移的行永远不自动刷新——那需要对照原版逐条复核替换逻辑。
internal static partial class Program
{
	private const string GuardReferenceTarget = "0.111.0";

	private static void VanillaCopyGuardFreezesEntriesAndAsyncBodies()
	{
		string testsRoot = Path.GetFullPath(Path.Combine(FindTestsSourceDirectory(), ".."));
		string currentPath = Path.Combine(testsRoot, $"vanilla_copy_guard.{ModInfo.TargetGameVersion}.txt");
		string referencePath = Path.Combine(testsRoot, $"vanilla_copy_guard.{GuardReferenceTarget}.txt");
		Dictionary<string, string> current = ReadGuardTable(currentPath);
		IEnumerable<string> entryKeys = ReadGuardTable(referencePath).Keys
			.Concat(current.Keys)
			.Where(static key => !key.EndsWith("::MoveNext()", StringComparison.Ordinal))
			.Distinct(StringComparer.Ordinal);

		List<string> missing = [];
		List<string> drifted = [];
		foreach (string entryKey in entryKeys)
		{
			if (ResolveGuardKey(entryKey) is not MethodBase entry)
			{
				// 目标在本版本不存在（补丁按版本 #if 区分）；运行时守卫只校验实际打上的目标。
				continue;
			}

			foreach (MethodBase method in HextechVanillaCopyGuard.WithAsyncBody(entry))
			{
				string key = HextechVanillaCopyGuard.DescribeTarget(method);
				string hash = HextechVanillaCopyGuard.ComputeIlHash(method) ?? "<no body>";
				if (!current.TryGetValue(key, out string? frozen))
				{
					missing.Add($"{key}={hash}");
				}
				else if (!string.Equals(frozen, hash, StringComparison.Ordinal))
				{
					drifted.Add($"{key} frozen={frozen} actual={hash}");
				}
			}
		}

		if (Environment.GetEnvironmentVariable("HEXTECH_WRITE_PATCH_MANIFEST") == "1" && missing.Count > 0)
		{
			File.AppendAllLines(currentPath, missing.OrderBy(static row => row, StringComparer.Ordinal));
			missing.Clear();
		}

		Expect(drifted.Count == 0, "vanilla copy guard drift needs review:\n  " + string.Join("\n  ", drifted));
		Expect(missing.Count == 0, "vanilla copy guard is missing rows (regenerate with HEXTECH_WRITE_PATCH_MANIFEST=1):\n  " + string.Join("\n  ", missing));
	}

	private static Dictionary<string, string> ReadGuardTable(string path)
	{
		Dictionary<string, string> rows = new(StringComparer.Ordinal);
		if (!File.Exists(path))
		{
			return rows;
		}

		foreach (string line in File.ReadAllLines(path))
		{
			string trimmed = line.Trim();
			int separator = trimmed.LastIndexOf('=');
			if (trimmed.Length > 0 && !trimmed.StartsWith('#') && separator > 0)
			{
				rows[trimmed[..separator]] = trimmed[(separator + 1)..];
			}
		}

		return rows;
	}

	private static MethodBase? ResolveGuardKey(string key)
	{
		int split = key.IndexOf("::", StringComparison.Ordinal);
		int open = key.IndexOf('(', split);
		if (split <= 0 || open < 0 || !key.EndsWith(')'))
		{
			throw new InvalidOperationException("malformed vanilla copy guard key: " + key);
		}

		string typeName = key[..split];
		string methodName = key[(split + 2)..open];
		string parameters = key[(open + 1)..^1];
		Type? type = AppDomain.CurrentDomain.GetAssemblies()
			.Select(assembly => assembly.GetType(typeName))
			.FirstOrDefault(static candidate => candidate != null);
		if (type == null)
		{
			return null;
		}

		const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
		IEnumerable<MethodBase> candidates = methodName == ".ctor"
			? type.GetConstructors(flags)
			: type.GetMethods(flags).Where(method => method.Name == methodName);
		return candidates.FirstOrDefault(method => string.Join(",", method.GetParameters()
			.Select(static parameter => parameter.ParameterType.FullName ?? parameter.ParameterType.Name)) == parameters);
	}
}
