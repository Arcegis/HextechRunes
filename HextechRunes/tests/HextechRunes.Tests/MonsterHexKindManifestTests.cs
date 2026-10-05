using System.Text;
using HextechRunes;

namespace HextechRunes.Tests;

internal static partial class Program
{
	private const string MonsterHexKindManifestFileName = "monster_hex_kind_manifest.txt";
	private const string RetiredMonsterHexKindPrefix = "retired=";

	/// <summary>
	/// <see cref="MonsterHexKind"/> 的数值是配置、遥测与联机载荷里的身份,只能追加。清单逐行记 <c>名称=数值</c>,
	/// 已删除的数值记 <c>retired=数值</c>,不得复用。已有行一旦缺失(改名、改号、删除)就失败,不会被自动刷新;
	/// 新增值必须大于清单里所有数值,用 HEXTECH_WRITE_PATCH_MANIFEST=1 追加到清单末尾。
	/// </summary>
	[HextechTest]
	private static void MonsterHexKindManifestIsAppendOnly()
	{
		string manifestPath = Path.Combine(FindTestsSourceDirectory(), MonsterHexKindManifestFileName);
		Expect(File.Exists(manifestPath), $"{MonsterHexKindManifestFileName} should exist at {manifestPath}");
		string[] rows = File.ReadAllLines(manifestPath)
			.Select(static line => line.Trim())
			.Where(static line => line.Length > 0 && !line.StartsWith('#'))
			.ToArray();
		HashSet<int> retired = rows
			.Where(static row => row.StartsWith(RetiredMonsterHexKindPrefix, StringComparison.Ordinal))
			.Select(static row => int.Parse(row[RetiredMonsterHexKindPrefix.Length..], System.Globalization.CultureInfo.InvariantCulture))
			.ToHashSet();
		string[] expected = rows
			.Where(static row => !row.StartsWith(RetiredMonsterHexKindPrefix, StringComparison.Ordinal))
			.ToArray();
		int highestRecorded = rows
			.Select(static row => int.Parse(row[(row.IndexOf('=') + 1)..], System.Globalization.CultureInfo.InvariantCulture))
			.DefaultIfEmpty(-1)
			.Max();

		MonsterHexKind[] kinds = Enum.GetValues<MonsterHexKind>();
		string[] actual = kinds
			.OrderBy(static kind => (int)kind)
			.Select(static kind => $"{kind}={(int)kind}")
			.ToArray();
		string[] removed = expected.Except(actual, StringComparer.Ordinal).ToArray();
		string[] reused = kinds.Where(kind => retired.Contains((int)kind)).Select(static kind => $"{kind}={(int)kind}").ToArray();
		string[] added = actual.Except(expected, StringComparer.Ordinal).ToArray();
		string[] notAppended = added
			.Where(row => int.Parse(row[(row.IndexOf('=') + 1)..], System.Globalization.CultureInfo.InvariantCulture) <= highestRecorded)
			.ToArray();

		Expect(removed.Length == 0, "MonsterHexKind rows were renamed, renumbered or removed (retire a deleted number with a 'retired=N' row instead):\n  " + string.Join("\n  ", removed));
		Expect(reused.Length == 0, "MonsterHexKind reuses retired numbers:\n  " + string.Join("\n  ", reused));
		Expect(notAppended.Length == 0, $"new MonsterHexKind values must be appended above {highestRecorded}:\n  " + string.Join("\n  ", notAppended));
		if (Environment.GetEnvironmentVariable("HEXTECH_WRITE_PATCH_MANIFEST") == "1" && added.Length > 0)
		{
			File.AppendAllLines(manifestPath, added, Encoding.UTF8);
			Console.WriteLine($"monster hex kind manifest appended: {manifestPath}");
			added = [];
		}

		Expect(added.Length == 0, "MonsterHexKind manifest is missing new rows (append with HEXTECH_WRITE_PATCH_MANIFEST=1):\n  " + string.Join("\n  ", added));
	}

}
