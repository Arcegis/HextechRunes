namespace HextechRunes;

internal sealed class HextechEnemyHexCountState
{
	private int[] _counts = HextechRuneConfiguration.GetDefaultEnemyHexCountsByAct();

	public int[] Snapshot
	{
		get => _counts.ToArray();
		set => Set(value);
	}

	public void ResetToDefault()
	{
		_counts = HextechRuneConfiguration.GetDefaultEnemyHexCountsByAct();
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
