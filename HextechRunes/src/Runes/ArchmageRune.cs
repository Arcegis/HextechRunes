using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

public sealed class ArchmageRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ChancePercent", 33m)
	];

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| !CombatManager.Instance.IsInProgress
			|| !IsOwnedSkill(cardPlay.Card)
			|| !RollTrigger(cardPlay.Card)
			|| PickCardToMakeFree(cardPlay.Card) is not CardModel card)
		{
			return Task.CompletedTask;
		}

		card.SetToFreeThisTurn();
		Flash();
		return Task.CompletedTask;
	}

	private bool RollTrigger(CardModel sourceCard)
	{
		return Owner != null && HextechStableRandom.PercentChance(
			(RunState)Owner.RunState,
			DynamicVars["ChancePercent"].IntValue,
			"archmage-free-card",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
			CombatManager.Instance.History.Entries.Count().ToString(),
			HextechStableRandom.CardKey(sourceCard));
	}

	private CardModel? PickCardToMakeFree(CardModel sourceCard)
	{
		if (Owner == null)
		{
			return null;
		}

		IReadOnlyList<CardModel> handCards = PileType.Hand.GetPile(Owner).Cards;
		return PickFromCandidates(
				handCards
					.Where(static card => (card.EnergyCost.GetWithModifiers(CostModifiers.None) > 0 || card.BaseStarCost > 0)
						&& card.CostsEnergyOrStars(includeGlobalModifiers: true))
					.ToList(),
				sourceCard,
				"base-cost")
			?? PickFromCandidates(
				handCards.Where(static card => card.CostsEnergyOrStars(includeGlobalModifiers: true)).ToList(),
				sourceCard,
				"global-cost")
			?? PickFromCandidates(
				handCards
					.Where(static card => card.EnergyCost.GetWithModifiers(CostModifiers.None) > 0 || card.BaseStarCost > 0)
					.ToList(),
				sourceCard,
				"base-any")
			?? PickFromCandidates(handCards.ToList(), sourceCard, "any");
	}

	private CardModel? PickFromCandidates(IReadOnlyList<CardModel> candidates, CardModel sourceCard, string tier)
	{
		if (Owner == null || candidates.Count == 0)
		{
			return null;
		}

		int index = HextechStableRandom.Index(
			(RunState)Owner.RunState,
			candidates.Count,
			"archmage-pick-card",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
			CombatManager.Instance.History.Entries.Count().ToString(),
			HextechStableRandom.CardKey(sourceCard),
			tier,
			HextechStableRandom.CardPileKey(candidates));
		return candidates[index];
	}
}
