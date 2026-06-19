namespace HextechRunes;

internal sealed class HextechMayhemRunContext
{
	public HextechMayhemActState ActState { get; } = new();
	public HextechMayhemCombatTrackingState CombatTracking { get; } = new();
	public HextechMayhemChoiceHistoryState ChoiceHistory { get; } = new();
	public HextechActiveMonsterHexCache ActiveMonsterHexCache { get; } = new();
	public HextechEnemyHexCountState EnemyHexCounts { get; } = new();
	public HextechPlayerRuneConfigSnapshotState PlayerRuneConfig { get; } = new();
	public int HexCountRecoveryBaseline { get; set; }
	public int MonsterHexStrengthTierFloor { get; set; }
	public int EnemyTezcatarasMercyCombatCounter { get; set; }
	public bool HostUsesBetterMultiplayerScaling { get; set; }

	public bool IsEndlessLoopActive => MonsterHexStrengthTierFloor >= 3;

	public void ResetForNewRun(IReadOnlyList<int> enemyHexCountsByAct)
	{
		EnemyHexCounts.Set(enemyHexCountsByAct);
		ResetProgressState(hexCountRecoveryBaseline: 0, monsterHexStrengthTierFloor: 0);
		ActState.Reset();
		ChoiceHistory.Reset();
		ResetCombatTracking();
		ActiveMonsterHexCache.Invalidate();
	}

	public void ResetForEndlessLoop(int hexCountRecoveryBaseline)
	{
		ResetProgressState(hexCountRecoveryBaseline, monsterHexStrengthTierFloor: 3);
		ActState.ResetForEndlessLoop();
		ChoiceHistory.Reset();
		ResetCombatTracking();
		ActiveMonsterHexCache.Invalidate();
	}

	public void ResetForDebugMonsterHex(int actIndex, MonsterHexKind hex, HextechRarityTier rarity)
	{
		EnemyHexCounts.ResetToDefault();
		ResetProgressState(hexCountRecoveryBaseline: 0, monsterHexStrengthTierFloor: 0);
		ActState.DebugSetOnlyMonsterHex(actIndex, hex, rarity);
		ChoiceHistory.Reset();
		ResetCombatTracking();
		ActiveMonsterHexCache.Invalidate();
	}

	public void ResetCombatTracking()
	{
		CombatTracking.Reset();
	}

	private void ResetProgressState(int hexCountRecoveryBaseline, int monsterHexStrengthTierFloor)
	{
		HexCountRecoveryBaseline = hexCountRecoveryBaseline;
		MonsterHexStrengthTierFloor = monsterHexStrengthTierFloor;
		EnemyTezcatarasMercyCombatCounter = 0;
	}
}
