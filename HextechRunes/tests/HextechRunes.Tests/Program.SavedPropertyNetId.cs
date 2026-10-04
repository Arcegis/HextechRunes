#if STS2_107_1
namespace HextechRunes.Tests;

// SavedProperty net-id 规范化只编译进 0.107.1 变体(0.109 起由游戏自己做确定性排序)。
internal static partial class Program
{
	[HextechTest]
	private static void SavedPropertyNetIdCanonicalizationIsInjectionOrderIndependent()
	{
		IReadOnlySet<string> vanilla = new HashSet<string>(StringComparer.Ordinal) { "V0", "V1", "V2" };

		// 同一组模组属性名,但两端注入顺序不同(模拟不同的本地模组加载顺序)。
		List<string> mapClientA = ["V0", "V1", "V2", "Zebra", "Apple", "SavedTriggeredThisTurn", "Mango"];
		List<string> mapClientB = ["V0", "V1", "V2", "SavedTriggeredThisTurn", "Mango", "Apple", "Zebra"];

		List<string> canonicalA = HextechSavedPropertyNetIdCanonicalizer.Canonicalize(mapClientA, vanilla);
		List<string> canonicalB = HextechSavedPropertyNetIdCanonicalizer.Canonicalize(mapClientB, vanilla);

		Equal(string.Join(",", canonicalA), string.Join(",", canonicalB), "two clients with the same mod set must produce identical net-id layout regardless of injection order");

		// 原版前缀按原顺序保留(net-id 0..2 不变)。
		Equal("V0,V1,V2", string.Join(",", canonicalA.Take(3)), "vanilla prefix preserved in original order");
		// 模组后缀按序数排序。
		Equal("Apple,Mango,SavedTriggeredThisTurn,Zebra", string.Join(",", canonicalA.Skip(3)), "modded suffix is ordinal-sorted");
		// 条目总数不变(位宽不会因规范化漂移)。
		Equal(mapClientA.Count, canonicalA.Count, "canonicalization preserves entry count");
	}

	[HextechTest]
	private static void SavedPropertyNetIdBitSizeMatchesGameFormula()
	{
		// 必须与游戏 / RitsuLib 的 CeilToInt(Log2(count)) 完全一致。
		Equal(0, HextechSavedPropertyNetIdCanonicalizer.ComputeNetIdBitSize(1), "count=1 -> 0 bits (matches game)");
		Equal(1, HextechSavedPropertyNetIdCanonicalizer.ComputeNetIdBitSize(2), "count=2 -> 1 bit");
		Equal(3, HextechSavedPropertyNetIdCanonicalizer.ComputeNetIdBitSize(7), "count=7 -> ceil(log2 7)=3 bits");
		Equal(7, HextechSavedPropertyNetIdCanonicalizer.ComputeNetIdBitSize(128), "count=128 -> 7 bits");
		Equal(8, HextechSavedPropertyNetIdCanonicalizer.ComputeNetIdBitSize(129), "count=129 -> 8 bits");
	}
}
#endif
