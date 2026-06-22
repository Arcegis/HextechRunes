namespace HextechRunes;

internal sealed class HextechPlayerHexCountState
{
	private int[] _counts = HextechRuneConfiguration.GetDefaultPlayerHexCountsByAct();

	public int[] Snapshot
	{
		get => _counts.ToArray();
		set => Set(value);
	}

	public void ResetToDefault()
	{
		_counts = HextechRuneConfiguration.GetDefaultPlayerHexCountsByAct();
	}

	public void Set(IReadOnlyList<int>? counts)
	{
		_counts = Normalize(counts);
	}

	public int GetForAct(int actIndex, bool endless)
	{
		int slot = endless ? 2 : Math.Clamp(actIndex, 0, _counts.Length - 1);
		return _counts[slot];
	}

	public static int[] Normalize(IReadOnlyList<int>? counts)
	{
		return HextechRuneConfiguration.NormalizePlayerHexCounts(counts);
	}
}
