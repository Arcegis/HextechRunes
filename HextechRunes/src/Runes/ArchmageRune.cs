namespace HextechRunes;

public sealed class ArchmageRune : HextechRelicBase
{
	private int _freeCardRollsThisCombat;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ChancePercent", 33m)
	];

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| !CombatManager.Instance.IsInProgress
			|| !IsOwnedSkill(cardPlay.Card)
			|| !RollTrigger(cardPlay.Card, out int rollOrdinal)
			|| PickCardToMakeFree(cardPlay.Card, rollOrdinal) is not CardModel card)
		{
			return Task.CompletedTask;
		}

		card.SetToFreeThisTurn();
		Flash();
		return Task.CompletedTask;
	}

	private bool RollTrigger(CardModel sourceCard, out int rollOrdinal)
	{
		rollOrdinal = -1;
		if (Owner == null)
		{
			return false;
		}

		rollOrdinal = ConsumeCombatProcOrdinal(nameof(ArchmageRune), ref _freeCardRollsThisCombat);
		return HextechStableRandom.PercentChance(
			(RunState)Owner.RunState,
			DynamicVars["ChancePercent"].IntValue,
			"archmage-free-card",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
			rollOrdinal.ToString(),
			HextechStableRandom.CardKey(sourceCard));
	}

	private CardModel? PickCardToMakeFree(CardModel sourceCard, int rollOrdinal)
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
					rollOrdinal,
					"base-cost")
				?? PickFromCandidates(
					handCards.Where(static card => card.CostsEnergyOrStars(includeGlobalModifiers: true)).ToList(),
					sourceCard,
					rollOrdinal,
					"global-cost")
			?? PickFromCandidates(
				handCards
					.Where(static card => card.EnergyCost.GetWithModifiers(CostModifiers.None) > 0 || card.BaseStarCost > 0)
					.ToList(),
					sourceCard,
					rollOrdinal,
					"base-any")
				?? PickFromCandidates(handCards.ToList(), sourceCard, rollOrdinal, "any");
	}

	private CardModel? PickFromCandidates(IReadOnlyList<CardModel> candidates, CardModel sourceCard, int rollOrdinal, string tier)
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
				rollOrdinal.ToString(),
				HextechStableRandom.CardKey(sourceCard),
			tier,
			HextechStableRandom.CardPileKey(candidates));
		return candidates[index];
	}
}
