namespace HextechRunes;

/// <summary>
/// 每幕玩家/敌方海克斯数量(三幕各一个值)。normalize(null) 即默认值;存档与联机快照读写都经它归一化。
/// </summary>
internal sealed class HextechHexCountState(Func<IReadOnlyList<int>?, int[]> normalize)
{
	// 无尽循环中每一轮都按第 3 幕的数量结算。
	private const int EndlessLoopSlot = 2;

	private int[] _counts = normalize(null);

	public int[] Snapshot
	{
		get => _counts.ToArray();
		set => Set(value);
	}

	public void ResetToDefault()
	{
		_counts = normalize(null);
	}

	public void Set(IReadOnlyList<int>? counts)
	{
		_counts = normalize(counts);
	}

	public int GetForAct(int actIndex, bool endless)
	{
		return GetForAct(_counts, actIndex, endless);
	}

	internal static int GetForAct(IReadOnlyList<int> counts, int actIndex, bool endless)
	{
		return counts[endless ? EndlessLoopSlot : Math.Clamp(actIndex, 0, counts.Count - 1)];
	}

	public static int[] NormalizeEnemyCounts(IReadOnlyList<int>? counts)
	{
		int[] normalized = HextechRuneConfiguration.GetDefaultEnemyHexCountsByAct();
		if (counts == null)
		{
			return normalized;
		}

		for (int i = 0; i < Math.Min(normalized.Length, counts.Count); i++)
		{
			normalized[i] = HextechRuneConfiguration.ClampEnemyHexCount(counts[i]);
		}

		return normalized;
	}
}
