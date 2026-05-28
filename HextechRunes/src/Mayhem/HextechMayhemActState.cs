namespace HextechRunes;

internal sealed class HextechMayhemActState
{
	private int[] _rarityByAct = NewUnknownArray();
	private int[] _monsterHexByAct = NewUnknownArray();
	private int[] _resolvedActs = NewResolvedArray();
	private List<MonsterHexKind> _carriedMonsterHexes = new();

	public int ActCount => _resolvedActs.Length;

	public int[] SavedRarityByAct
	{
		get => _rarityByAct;
		set => _rarityByAct = NormalizeUnknownArray(value);
	}

	public int[] SavedMonsterHexByAct
	{
		get => _monsterHexByAct;
		set => _monsterHexByAct = NormalizeUnknownArray(value);
	}

	public int[] SavedCarriedMonsterHexes
	{
		get => _carriedMonsterHexes.Select(static hex => (int)hex).ToArray();
		set => _carriedMonsterHexes = NormalizeMonsterHexList(value);
	}

	public int[] SavedResolvedActs
	{
		get => _resolvedActs;
		set => _resolvedActs = NormalizeResolvedArray(value);
	}

	public bool IsResolved(int actIndex)
	{
		return actIndex >= 0 && actIndex < _resolvedActs.Length && _resolvedActs[actIndex] > 0;
	}

	public void SetResolved(int actIndex, bool resolved)
	{
		if (actIndex >= 0 && actIndex < _resolvedActs.Length)
		{
			_resolvedActs[actIndex] = resolved ? 1 : 0;
		}
	}

	public bool TryMarkResolved(int actIndex)
	{
		if (actIndex < 0 || actIndex >= _resolvedActs.Length || _resolvedActs[actIndex] > 0)
		{
			return false;
		}

		_resolvedActs[actIndex] = 1;
		return true;
	}

	public HextechRarityTier? GetRarity(int actIndex)
	{
		if (actIndex < 0 || actIndex >= _rarityByAct.Length || _rarityByAct[actIndex] < 0)
		{
			return null;
		}

		return (HextechRarityTier)_rarityByAct[actIndex];
	}

	public void SetRarity(int actIndex, HextechRarityTier rarity)
	{
		if (actIndex >= 0 && actIndex < _rarityByAct.Length)
		{
			_rarityByAct[actIndex] = (int)rarity;
		}
	}

	public bool TrySetRarityIfMissing(int actIndex, HextechRarityTier rarity)
	{
		if (actIndex < 0 || actIndex >= _rarityByAct.Length || _rarityByAct[actIndex] >= 0)
		{
			return false;
		}

		_rarityByAct[actIndex] = (int)rarity;
		return true;
	}

	public MonsterHexKind? GetMonsterHex(int actIndex)
	{
		if (actIndex < 0 || actIndex >= _monsterHexByAct.Length || _monsterHexByAct[actIndex] < 0)
		{
			return null;
		}

		return (MonsterHexKind)_monsterHexByAct[actIndex];
	}

	public void SetMonsterHex(int actIndex, MonsterHexKind hex)
	{
		if (actIndex >= 0 && actIndex < _monsterHexByAct.Length)
		{
			_monsterHexByAct[actIndex] = (int)hex;
		}
	}

	public void ClearMonsterHex(int actIndex)
	{
		if (actIndex >= 0 && actIndex < _monsterHexByAct.Length)
		{
			_monsterHexByAct[actIndex] = -1;
		}
	}

	public IReadOnlyList<MonsterHexKind> GetActiveMonsterHexes(int currentActIndex, Func<int, bool> shouldRecoverMonsterHex)
	{
		List<MonsterHexKind> result = new();
		HashSet<MonsterHexKind> seen = new();
		foreach (MonsterHexKind hex in _carriedMonsterHexes)
		{
			if (seen.Add(hex))
			{
				result.Add(hex);
			}
		}

		for (int actIndex = 0; actIndex <= currentActIndex && actIndex < _monsterHexByAct.Length; actIndex++)
		{
			if (_monsterHexByAct[actIndex] >= 0
				&& (IsResolved(actIndex) || shouldRecoverMonsterHex(actIndex)))
			{
				MonsterHexKind hex = (MonsterHexKind)_monsterHexByAct[actIndex];
				if (seen.Add(hex))
				{
					result.Add(hex);
				}
			}
		}

		return result;
	}

	public IReadOnlyList<MonsterHexKind> GetKnownMonsterHexes()
	{
		List<MonsterHexKind> result = new();
		HashSet<MonsterHexKind> seen = new();
		foreach (MonsterHexKind hex in _carriedMonsterHexes)
		{
			if (seen.Add(hex))
			{
				result.Add(hex);
			}
		}

		foreach (int rawHex in _monsterHexByAct)
		{
			if (rawHex >= 0 && Enum.IsDefined(typeof(MonsterHexKind), rawHex))
			{
				MonsterHexKind hex = (MonsterHexKind)rawHex;
				if (seen.Add(hex))
				{
					result.Add(hex);
				}
			}
		}

		return result;
	}

	public int LastActIndexFor(int maxActIndex)
	{
		return Math.Min(maxActIndex, _resolvedActs.Length - 1);
	}

	public void Reset()
	{
		_rarityByAct = NewUnknownArray();
		_monsterHexByAct = NewUnknownArray();
		_resolvedActs = NewResolvedArray();
		_carriedMonsterHexes.Clear();
	}

	public void ResetForEndlessLoop()
	{
		CarryResolvedMonsterHexes();
		_rarityByAct = NewUnknownArray();
		_monsterHexByAct = NewUnknownArray();
		_resolvedActs = NewResolvedArray();
	}

	public void DebugSetOnlyMonsterHex(int actIndex, MonsterHexKind hex, HextechRarityTier rarity)
	{
		Reset();
		if (actIndex >= 0 && actIndex < _monsterHexByAct.Length)
		{
			_rarityByAct[actIndex] = (int)rarity;
			_monsterHexByAct[actIndex] = (int)hex;
			_resolvedActs[actIndex] = 1;
		}
	}

	public string Describe()
	{
		return $"resolved={string.Join(",", _resolvedActs)} rarity={string.Join(",", _rarityByAct)} monster={string.Join(",", _monsterHexByAct)} carried={string.Join(",", _carriedMonsterHexes)}";
	}

	private void CarryResolvedMonsterHexes()
	{
		HashSet<MonsterHexKind> seen = _carriedMonsterHexes.ToHashSet();
		for (int actIndex = 0; actIndex < _monsterHexByAct.Length; actIndex++)
		{
			int rawHex = _monsterHexByAct[actIndex];
			if (rawHex < 0 || !IsResolved(actIndex) || !Enum.IsDefined(typeof(MonsterHexKind), rawHex))
			{
				continue;
			}

			MonsterHexKind hex = (MonsterHexKind)rawHex;
			if (seen.Add(hex))
			{
				_carriedMonsterHexes.Add(hex);
			}
		}
	}

	private static int[] NewUnknownArray()
	{
		return [ -1, -1, -1 ];
	}

	private static int[] NewResolvedArray()
	{
		return [ 0, 0, 0 ];
	}

	private static int[] NormalizeUnknownArray(int[]? value)
	{
		int[] normalized = NewUnknownArray();
		if (value == null)
		{
			return normalized;
		}

		for (int i = 0; i < Math.Min(normalized.Length, value.Length); i++)
		{
			normalized[i] = value[i];
		}

		return normalized;
	}

	private static int[] NormalizeResolvedArray(int[]? value)
	{
		int[] normalized = NewResolvedArray();
		if (value == null)
		{
			return normalized;
		}

		for (int i = 0; i < Math.Min(normalized.Length, value.Length); i++)
		{
			normalized[i] = value[i] > 0 ? 1 : 0;
		}

		return normalized;
	}

	private static List<MonsterHexKind> NormalizeMonsterHexList(int[]? value)
	{
		List<MonsterHexKind> normalized = new();
		HashSet<MonsterHexKind> seen = new();
		if (value == null)
		{
			return normalized;
		}

		foreach (int rawHex in value)
		{
			if (!Enum.IsDefined(typeof(MonsterHexKind), rawHex))
			{
				continue;
			}

			MonsterHexKind hex = (MonsterHexKind)rawHex;
			if (seen.Add(hex))
			{
				normalized.Add(hex);
			}
		}

		return normalized;
	}
}
