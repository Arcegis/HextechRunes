namespace HextechRunes;

public sealed class GrowingStrongerRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (Owner == null
			|| power.Owner != Owner.Creature
			|| power.GetType() != typeof(StrengthPower)
			|| amount <= 0m
			|| Owner.PlayerCombatState == null)
		{
			return Task.CompletedTask;
		}

		int cardsToFree = FloorToInt(amount);
		if (cardsToFree <= 0)
		{
			return Task.CompletedTask;
		}

		bool freedAny = false;
		for (int i = 0; i < cardsToFree; i++)
		{
			CardModel? card = PickCardToMakeFree(i, cardsToFree);
			if (card == null)
			{
				break;
			}

			card.SetToFreeThisTurn();
			freedAny = true;
		}

		if (freedAny)
		{
			Flash();
		}

		return Task.CompletedTask;
	}

	private CardModel? PickCardToMakeFree(int ordinal, int total)
	{
		if (Owner?.PlayerCombatState == null)
		{
			return null;
		}

		return HextechFreeCardPicker.Pick(
			PileType.Hand.GetPile(Owner).Cards,
			(candidates, tier) => PickCardToMakeFreeFromCandidates(candidates, ordinal, total, tier));
	}

	private CardModel? PickCardToMakeFreeFromCandidates(IReadOnlyList<CardModel> candidates, int ordinal, int total, string tier)
	{
		if (Owner == null || candidates.Count == 0)
		{
			return null;
		}

		int index = HextechStableRandom.Index(
			(RunState)Owner.RunState,
			candidates.Count,
			"guinsoos-rageblade-free-card",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
			ordinal.ToString(),
			total.ToString(),
			tier,
			HextechStableRandom.CardPileKey(candidates));
		return candidates[index];
	}
}
