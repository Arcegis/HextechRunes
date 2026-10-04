namespace HextechRunes;

public sealed class NowYouSeeMeRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
	{
		if (Owner.Creature.IsDead
			|| !IsOwnedCard(card)
			|| card.Type is not (CardType.Status or CardType.Curse)
			|| card.Pile?.Type != PileType.Discard
			|| card.IsSlyThisTurn
			|| CombatManager.Instance.IsOverOrEnding
			|| !CombatManager.Instance.IsPartOfPlayerTurn(Owner))
		{
			return;
		}

		Flash();
		await CardCmd.Exhaust(choiceContext, card, causedByEthereal: false, skipVisuals: true);
	}
}
