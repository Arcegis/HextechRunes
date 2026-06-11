using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	internal Task RunGroupedPlayerDebuffBurst(Func<Task> action)
	{
		return HextechEnemyTriggerGuard.RunGroupedPlayerDebuffBurst(_combatTracking, action);
	}

	internal async Task TryApplyServantMasterIllusion(Creature creature, Creature? applier, CardModel? cardSource)
	{
		await HextechServantMasterIllusionService.TryApply(RunState, _combatTracking, creature, applier, cardSource);
	}

	internal int GetPlayerRuneProcsThisTurn(Player player, string procKey)
	{
		return HextechCombatProcTracker.GetPlayerRuneProcsThisTurn(_combatTracking, player, procKey);
	}

	internal bool TryConsumePlayerRuneProcThisTurn(Player player, string procKey, int maxPerTurn)
	{
		return HextechCombatProcTracker.TryConsumePlayerRuneProcThisTurn(_combatTracking, player, procKey, maxPerTurn);
	}

	internal int ConsumePlayerRuneProcInCombat(Player player, string procKey)
	{
		return HextechCombatProcTracker.ConsumePlayerRuneProcInCombat(_combatTracking, player, procKey);
	}

	private bool TrackPlayerAttackCardPlayedThisTurn(CardPlay cardPlay)
	{
		return HextechCombatProcTracker.TrackPlayerAttackCardPlayedThisTurn(_combatTracking, cardPlay);
	}

	internal void RefreshPlayerAttackCostDoublingPreviews(IEnumerable<Creature> playerCreatures)
	{
		HextechAttackCostPreviewRefresher.Refresh(this, RunState, playerCreatures);
	}

	internal int GetPlayerAttacksPlayedThisTurn(CardModel card)
	{
		return HextechCombatProcTracker.GetPlayerAttacksPlayedThisTurn(_combatTracking, card);
	}

	public decimal ModifyEnemyHealAmount(Creature creature, decimal amount)
	{
		return HextechEnemyHealModifier.Modify(this, creature, amount);
	}
}
