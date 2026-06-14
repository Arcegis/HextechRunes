namespace HextechRunes;

internal sealed class HextechActiveMonsterHexCache
{
	private IReadOnlyList<MonsterHexKind>? _activeHexes;
	private HashSet<MonsterHexKind>? _activeHexSet;
	private int _actIndex = int.MinValue;
	private bool _combatRecovery;

	public IReadOnlyList<MonsterHexKind> Get(
		HextechMayhemActState actState,
		int actIndex,
		Func<int, bool> shouldRecoverMonsterHexInCombat)
	{
		bool combatRecovery = shouldRecoverMonsterHexInCombat(actIndex);
		if (_activeHexes != null
			&& _activeHexSet != null
			&& _actIndex == actIndex
			&& _combatRecovery == combatRecovery)
		{
			return _activeHexes;
		}

		_activeHexes = actState.GetActiveMonsterHexes(actIndex, shouldRecoverMonsterHexInCombat);
		_activeHexSet = _activeHexes.ToHashSet();
		_actIndex = actIndex;
		_combatRecovery = combatRecovery;
		return _activeHexes;
	}

	public bool Contains(
		HextechMayhemActState actState,
		int actIndex,
		Func<int, bool> shouldRecoverMonsterHexInCombat,
		MonsterHexKind hex)
	{
		_ = Get(actState, actIndex, shouldRecoverMonsterHexInCombat);
		return _activeHexSet!.Contains(hex);
	}

	public void Invalidate()
	{
		_activeHexes = null;
		_activeHexSet = null;
		_actIndex = int.MinValue;
		_combatRecovery = false;
	}
}
