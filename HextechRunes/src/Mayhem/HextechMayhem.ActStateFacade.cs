using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Rooms;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	public bool IsActResolved(int actIndex)
	{
		return _actState.IsResolved(actIndex);
	}

	public void SetActResolved(int actIndex, bool resolved)
	{
		_actState.SetResolved(actIndex, resolved);
		InvalidateActiveMonsterHexCache();
	}

	public bool TryRecoverResolvedActsFromPlayerRelics(string reason)
	{
		HextechMayhemActRecoveryResult recovery = HextechMayhemActRecovery.RecoverResolvedActs(
			RunState,
			_actState,
			_choiceHistory,
			_hexCountRecoveryBaseline);
		if (recovery.Changed)
		{
			InvalidateActiveMonsterHexCache();
			Log.Info($"[{ModInfo.Id}][Mayhem] Recovered resolved acts from saved choices/player relics: reason={reason} currentAct={RunState.CurrentActIndex} recoverThrough={recovery.RecoverThroughAct} telemetryThrough={recovery.TelemetryRecoverThroughAct} countThrough={recovery.CountRecoverThroughAct} baseline={_hexCountRecoveryBaseline} {_actState.Describe()} counts={DescribePlayerHexCounts()} choices={DescribeTelemetryChoiceCounts()}");
		}

		return recovery.Changed;
	}

	public string DescribeActState()
	{
		return _actState.Describe();
	}

	public HextechRarityTier? GetRarityForAct(int actIndex)
	{
		return _actState.GetRarity(actIndex);
	}

	public void SetRarityForAct(int actIndex, HextechRarityTier rarity)
	{
		_actState.SetRarity(actIndex, rarity);
		InvalidateActiveMonsterHexCache();
	}

	public MonsterHexKind? GetMonsterHexForAct(int actIndex)
	{
		return _actState.GetMonsterHex(actIndex);
	}

	public void SetMonsterHexForAct(int actIndex, MonsterHexKind hex)
	{
		_actState.SetMonsterHex(actIndex, hex);
		InvalidateActiveMonsterHexCache();
	}

	public void ClearMonsterHexForAct(int actIndex)
	{
		_actState.ClearMonsterHex(actIndex);
		InvalidateActiveMonsterHexCache();
	}

	public IReadOnlyList<MonsterHexKind> GetActiveMonsterHexes()
	{
		return _activeMonsterHexCache.Get(_actState, RunState.CurrentActIndex, ShouldRecoverMonsterHexInCombat);
	}

	public IReadOnlyList<MonsterHexKind> GetKnownMonsterHexes()
	{
		return _actState.GetKnownMonsterHexes();
	}

	private bool ShouldRecoverMonsterHexInCombat(int actIndex)
	{
		return actIndex <= RunState.CurrentActIndex && RunState.CurrentRoom is CombatRoom;
	}

	public void ResetForNewRun()
	{
		_hexCountRecoveryBaseline = 0;
		_monsterHexStrengthTierFloor = 0;
		_enemyTezcatarasMercyCombatCounter = 0;
		_actState.Reset();
		_choiceHistory.Reset();
		ResetCombatTracking();
		InvalidateActiveMonsterHexCache();
	}

	public void ResetForEndlessLoop(string reason)
	{
		_hexCountRecoveryBaseline = HextechMayhemActRecovery.GetMinimumPlayerHexCount(RunState);
		_monsterHexStrengthTierFloor = 3;
		_enemyTezcatarasMercyCombatCounter = 0;
		_actState.ResetForEndlessLoop();
		_choiceHistory.Reset();
		ResetCombatTracking();
		InvalidateActiveMonsterHexCache();
		Log.Info($"[{ModInfo.Id}][Mayhem] Reset for endless loop: reason={reason} baseline={_hexCountRecoveryBaseline} strengthTierFloor={_monsterHexStrengthTierFloor} counts={DescribePlayerHexCounts()} {_actState.Describe()}");
		HextechRunLifecycleHooks.HandleEndlessLoopReset(this, reason);
	}

	public void DebugSetOnlyMonsterHex(int actIndex, MonsterHexKind hex, HextechRarityTier rarity)
	{
		_hexCountRecoveryBaseline = 0;
		_monsterHexStrengthTierFloor = 0;
		_enemyTezcatarasMercyCombatCounter = 0;
		_actState.DebugSetOnlyMonsterHex(actIndex, hex, rarity);
		_choiceHistory.Reset();

		ResetCombatTracking();
		InvalidateActiveMonsterHexCache();
	}

	public bool DebugAddMonsterHex(MonsterHexKind hex)
	{
		bool changed = _actState.AddCarriedMonsterHex(hex);
		if (changed)
		{
			InvalidateActiveMonsterHexCache();
		}

		return changed;
	}

	public bool DebugRemoveMonsterHex(MonsterHexKind hex)
	{
		bool changed = _actState.RemoveMonsterHexEverywhere(hex);
		if (changed)
		{
			InvalidateActiveMonsterHexCache();
		}

		return changed;
	}

	public bool HasActiveMonsterHex(MonsterHexKind hex)
	{
		return _activeMonsterHexCache.Contains(_actState, RunState.CurrentActIndex, ShouldRecoverMonsterHexInCombat, hex);
	}

	public int GetMonsterHexStrengthTier(MonsterHexKind hex)
	{
		return GetMonsterHexStrengthTierForAct(hex, RunState.CurrentActIndex);
	}

	public int GetMonsterHexStrengthTierForAct(MonsterHexKind hex, int actIndex)
	{
		_ = hex;
		// Enemy hex strength tracks the active act, even for hexes obtained in earlier acts.
		int actStrengthTier = Math.Clamp(actIndex + 1, 1, 3);
		return Math.Max(actStrengthTier, _monsterHexStrengthTierFloor);
	}

	private void InvalidateActiveMonsterHexCache()
	{
		_activeMonsterHexCache.Invalidate();
	}

	internal bool IncrementEnemyTezcatarasMercyCombatCounter(int interval)
	{
		_enemyTezcatarasMercyCombatCounter++;
		if (_enemyTezcatarasMercyCombatCounter < interval)
		{
			return false;
		}

		_enemyTezcatarasMercyCombatCounter = 0;
		return true;
	}
}
