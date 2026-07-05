namespace HextechRunes;

public sealed class BlackCandleRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		List<CardModel> curses = Owner.Deck.Cards
			.Where(static card => card.Type == CardType.Curse)
			.ToList();
		if (curses.Count == 0)
		{
			return;
		}

		Flash();
		foreach (CardModel curse in curses)
		{
			await CardPileCmd.RemoveFromDeck(curse, showPreview: false);
		}
	}

	public override bool ShouldAddToDeck(CardModel card)
	{
		return card.Owner != Owner || card.Type != CardType.Curse;
	}

	public override Task AfterAddToDeckPrevented(CardModel card)
	{
		if (card.Owner == Owner && card.Type == CardType.Curse)
		{
			Flash();
		}

		return Task.CompletedTask;
	}
}
