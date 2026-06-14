using System.Text.Json;

namespace HextechRunes;

internal sealed class HextechMayhemActState
{
	private const int ActCountValue = 3;
	private int[] _rarityByAct = NewUnknownArray();
	private List<MonsterHexKind>[] _monsterHexesByAct = NewMonsterHexLists();
	private int[] _resolvedActs = NewResolvedArray();
	private HashSet<int> _mapLengthReducedActs = new();
	private List<MonsterHexKind> _carriedMonsterHexes = new();

	public int ActCount => _resolvedActs.Length;

	public int[] SavedRarityByAct
	{
		get => _rarityByAct;
		set => _rarityByAct = NormalizeUnknownArray(value);
	}

	public int[] SavedMonsterHexByAct
	{
		get => _monsterHexesByAct
			.Select(static hexes => hexes.Count > 0 ? (int)hexes[0] : -1)
			.ToArray();
		set => MergeLegacyMonsterHexByAct(value);
	}

	public string SavedMonsterHexesByActJson
	{
		get => SerializeMonsterHexesByAct();
		set => RestoreMonsterHexesByAct(value);
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

	public int[] SavedMapLengthReducedActs
	{
		get => _mapLengthReducedActs.OrderBy(static actIndex => actIndex).ToArray();
		set => _mapLengthReducedActs = NormalizeActIndexSet(value);
	}

	public bool IsResolved(int actIndex)
	{
		int slot = ToActSlotOrInvalid(actIndex);
		return slot >= 0 && _resolvedActs[slot] > 0;
	}

	public void SetResolved(int actIndex, bool resolved)
	{
		int slot = ToActSlotOrInvalid(actIndex);
		if (slot >= 0)
		{
			_resolvedActs[slot] = resolved ? 1 : 0;
		}
	}

	public bool TryMarkResolved(int actIndex)
	{
		int slot = ToActSlotOrInvalid(actIndex);
		if (slot < 0 || _resolvedActs[slot] > 0)
		{
			return false;
		}

		_resolvedActs[slot] = 1;
		return true;
	}

	public bool IsMapLengthReduced(int actIndex)
	{
		return _mapLengthReducedActs.Contains(ToActSlot(actIndex));
	}

	public void MarkMapLengthReduced(int actIndex)
	{
		_mapLengthReducedActs.Add(ToActSlot(actIndex));
	}

	public HextechRarityTier? GetRarity(int actIndex)
	{
		int slot = ToActSlotOrInvalid(actIndex);
		if (slot < 0 || _rarityByAct[slot] < 0)
		{
			return null;
		}

		return (HextechRarityTier)_rarityByAct[slot];
	}

	public void SetRarity(int actIndex, HextechRarityTier rarity)
	{
		int slot = ToActSlotOrInvalid(actIndex);
		if (slot >= 0)
		{
			_rarityByAct[slot] = (int)rarity;
		}
	}

	public bool TrySetRarityIfMissing(int actIndex, HextechRarityTier rarity)
	{
		int slot = ToActSlotOrInvalid(actIndex);
		if (slot < 0 || _rarityByAct[slot] >= 0)
		{
			return false;
		}

		_rarityByAct[slot] = (int)rarity;
		return true;
	}

	public MonsterHexKind? GetMonsterHex(int actIndex)
	{
		IReadOnlyList<MonsterHexKind> hexes = GetMonsterHexes(actIndex);
		return hexes.Count > 0 ? hexes[0] : null;
	}

	public IReadOnlyList<MonsterHexKind> GetMonsterHexes(int actIndex)
	{
		int slot = ToActSlotOrInvalid(actIndex);
		return slot >= 0 ? _monsterHexesByAct[slot].ToArray() : [];
	}

	public void SetMonsterHex(int actIndex, MonsterHexKind hex)
	{
		SetMonsterHexes(actIndex, [ hex ]);
	}

	public void SetMonsterHexes(int actIndex, IEnumerable<MonsterHexKind> hexes)
	{
		int slot = ToActSlotOrInvalid(actIndex);
		if (slot >= 0)
		{
			_monsterHexesByAct[slot] = NormalizeMonsterHexList(hexes.Select(static hex => (int)hex));
		}
	}

	public void ClearMonsterHex(int actIndex)
	{
		int slot = ToActSlotOrInvalid(actIndex);
		if (slot >= 0)
		{
			_monsterHexesByAct[slot].Clear();
		}
	}

	public bool AddCarriedMonsterHex(MonsterHexKind hex)
	{
		if (_carriedMonsterHexes.Contains(hex))
		{
			return false;
		}

		_carriedMonsterHexes.Add(hex);
		return true;
	}

	public bool RemoveMonsterHexEverywhere(MonsterHexKind hex)
	{
		bool removed = _carriedMonsterHexes.RemoveAll(existing => existing == hex) > 0;
		foreach (List<MonsterHexKind> hexes in _monsterHexesByAct)
		{
			removed |= hexes.RemoveAll(existing => existing == hex) > 0;
		}

		return removed;
	}

	public IReadOnlyList<MonsterHexKind> GetActiveMonsterHexes(int currentActIndex, Func<int, bool> shouldRecoverMonsterHex)
	{
		List<MonsterHexKind> result = new();
		HashSet<MonsterHexKind> seen = new();
		AddUnique(result, seen, _carriedMonsterHexes);

		int latestSlot = LatestActiveSlot(ToActSlot(currentActIndex), shouldRecoverMonsterHex);
		if (latestSlot >= 0)
		{
			AddUnique(result, seen, _monsterHexesByAct[latestSlot]);
		}

		return result;
	}

	public IReadOnlyList<MonsterHexKind> GetActiveMonsterHexesBeforeAct(int actIndex)
	{
		List<MonsterHexKind> result = new();
		HashSet<MonsterHexKind> seen = new();
		AddUnique(result, seen, _carriedMonsterHexes);

		int previousSlot = Math.Min(ToActSlot(actIndex) - 1, ActCountValue - 1);
		for (int slot = previousSlot; slot >= 0; slot--)
		{
			if (_resolvedActs[slot] <= 0)
			{
				continue;
			}

			AddUnique(result, seen, _monsterHexesByAct[slot]);
			break;
		}

		return result;
	}

	public IReadOnlyList<MonsterHexKind> GetKnownMonsterHexes()
	{
		List<MonsterHexKind> result = new();
		HashSet<MonsterHexKind> seen = new();
		AddUnique(result, seen, _carriedMonsterHexes);
		foreach (List<MonsterHexKind> hexes in _monsterHexesByAct)
		{
			AddUnique(result, seen, hexes);
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
		_monsterHexesByAct = NewMonsterHexLists();
		_resolvedActs = NewResolvedArray();
		_mapLengthReducedActs.Clear();
		_carriedMonsterHexes.Clear();
	}

	public void ResetForEndlessLoop()
	{
		CarryActiveMonsterHexes();
		_rarityByAct = NewUnknownArray();
		_monsterHexesByAct = NewMonsterHexLists();
		_resolvedActs = NewResolvedArray();
		_mapLengthReducedActs.Clear();
	}

	public void DebugSetOnlyMonsterHex(int actIndex, MonsterHexKind hex, HextechRarityTier rarity)
	{
		Reset();
		int slot = ToActSlotOrInvalid(actIndex);
		if (slot >= 0)
		{
			_rarityByAct[slot] = (int)rarity;
			_monsterHexesByAct[slot] = [ hex ];
			_resolvedActs[slot] = 1;
		}
	}

	public string Describe()
	{
		string monster = string.Join(";", _monsterHexesByAct.Select(static (hexes, index) => $"{index}=[{string.Join(",", hexes)}]"));
		return $"resolved={string.Join(",", _resolvedActs)} rarity={string.Join(",", _rarityByAct)} monster={monster} carried={string.Join(",", _carriedMonsterHexes)} mapReduced={string.Join(",", _mapLengthReducedActs.OrderBy(static actIndex => actIndex))}";
	}

	private void MergeLegacyMonsterHexByAct(int[]? value)
	{
		if (value == null || _monsterHexesByAct.Any(static hexes => hexes.Count > 0))
		{
			return;
		}

		List<MonsterHexKind> cumulative = new();
		HashSet<MonsterHexKind> seen = new();
		for (int actIndex = 0; actIndex < Math.Min(ActCountValue, value.Length); actIndex++)
		{
			int rawHex = value[actIndex];
			if (Enum.IsDefined(typeof(MonsterHexKind), rawHex))
			{
				MonsterHexKind hex = (MonsterHexKind)rawHex;
				if (seen.Add(hex))
				{
					cumulative.Add(hex);
				}
			}

			_monsterHexesByAct[actIndex] = cumulative.ToList();
		}
	}

	private string SerializeMonsterHexesByAct()
	{
		int[][] raw = _monsterHexesByAct
			.Select(static hexes => hexes.Select(static hex => (int)hex).ToArray())
			.ToArray();
		return raw.Any(static hexes => hexes.Length > 0)
			? JsonSerializer.Serialize(raw)
			: "";
	}

	private void RestoreMonsterHexesByAct(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return;
		}

		try
		{
			int[][]? raw = JsonSerializer.Deserialize<int[][]>(json);
			if (raw == null)
			{
				return;
			}

			List<MonsterHexKind>[] normalized = NewMonsterHexLists();
			for (int actIndex = 0; actIndex < Math.Min(ActCountValue, raw.Length); actIndex++)
			{
				normalized[actIndex] = NormalizeMonsterHexList(raw[actIndex]);
			}

			_monsterHexesByAct = normalized;
		}
		catch
		{
			_monsterHexesByAct = NewMonsterHexLists();
		}
	}

	private void CarryActiveMonsterHexes()
	{
		int latestResolvedSlot = -1;
		for (int slot = 0; slot < _resolvedActs.Length; slot++)
		{
			if (_resolvedActs[slot] > 0)
			{
				latestResolvedSlot = slot;
			}
		}

		List<MonsterHexKind> carried = new();
		HashSet<MonsterHexKind> seen = new();
		AddUnique(carried, seen, _carriedMonsterHexes);
		if (latestResolvedSlot >= 0)
		{
			AddUnique(carried, seen, _monsterHexesByAct[latestResolvedSlot]);
		}

		_carriedMonsterHexes = carried;
	}

	private int LatestActiveSlot(int maxSlot, Func<int, bool> shouldRecoverMonsterHex)
	{
		for (int slot = Math.Min(maxSlot, ActCountValue - 1); slot >= 0; slot--)
		{
			if (_resolvedActs[slot] > 0 || shouldRecoverMonsterHex(slot))
			{
				return slot;
			}
		}

		return -1;
	}

	private static void AddUnique(List<MonsterHexKind> result, HashSet<MonsterHexKind> seen, IEnumerable<MonsterHexKind> hexes)
	{
		foreach (MonsterHexKind hex in hexes)
		{
			if (seen.Add(hex))
			{
				result.Add(hex);
			}
		}
	}

	private static int ToActSlot(int actIndex)
	{
		return Math.Clamp(actIndex, 0, ActCountValue - 1);
	}

	private static int ToActSlotOrInvalid(int actIndex)
	{
		return actIndex < 0 ? -1 : ToActSlot(actIndex);
	}

	private static int[] NewUnknownArray()
	{
		return [ -1, -1, -1 ];
	}

	private static int[] NewResolvedArray()
	{
		return [ 0, 0, 0 ];
	}

	private static List<MonsterHexKind>[] NewMonsterHexLists()
	{
		return [ [], [], [] ];
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

	private static List<MonsterHexKind> NormalizeMonsterHexList(IEnumerable<int>? value)
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

	private static HashSet<int> NormalizeActIndexSet(int[]? value)
	{
		HashSet<int> normalized = new();
		if (value == null)
		{
			return normalized;
		}

		foreach (int actIndex in value)
		{
			if (actIndex >= 0 && actIndex < ActCountValue)
			{
				normalized.Add(actIndex);
			}
		}

		return normalized;
	}
}
