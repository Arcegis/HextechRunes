namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	internal async Task TryApplyServantMasterIllusion(Creature creature, Creature? applier, CardModel? cardSource)
	{
		await HextechServantMasterIllusionService.TryApply(RunState, CombatTracking, creature, applier, cardSource);
	}

	internal int GetPlayerRuneProcsThisTurn(Player player, string procKey)
	{
		return HextechCombatProcTracker.GetPlayerRuneProcsThisTurn(CombatTracking, player, procKey);
	}

	internal bool TryConsumePlayerRuneProcThisTurn(Player player, string procKey, int maxPerTurn)
	{
		return HextechCombatProcTracker.TryConsumePlayerRuneProcThisTurn(CombatTracking, player, procKey, maxPerTurn);
	}

	internal int GetPlayerRuneProcsInCombat(Player player, string procKey)
	{
		return HextechCombatProcTracker.GetPlayerRuneProcsInCombat(CombatTracking, player, procKey);
	}

	internal int ConsumePlayerRuneProcInCombat(Player player, string procKey)
	{
		return HextechCombatProcTracker.ConsumePlayerRuneProcInCombat(CombatTracking, player, procKey);
	}

	internal int ConsumeGlobalProcInCombat(string procKey)
	{
		return HextechCombatProcTracker.ConsumeGlobalProcInCombat(CombatTracking, procKey);
	}

	private bool TrackPlayerAttackCardPlayedThisTurn(CardPlay cardPlay)
	{
		return HextechCombatProcTracker.TrackPlayerAttackCardPlayedThisTurn(CombatTracking, cardPlay);
	}

	internal void RefreshPlayerAttackCostDoublingPreviews(IEnumerable<Creature> playerCreatures)
	{
		HextechAttackCostPreviewRefresher.Refresh(this, RunState, playerCreatures);
	}

	internal int GetPlayerAttacksPlayedThisTurn(CardModel card)
	{
		return HextechCombatProcTracker.GetPlayerAttacksPlayedThisTurn(CombatTracking, card);
	}

	public decimal ModifyEnemyHealAmount(Creature creature, decimal amount)
	{
		return HextechEnemyHealModifier.Modify(this, creature, amount);
	}
}
