using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

public sealed class MirrorReflectionRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		List<CardPileAddResult> results = new();
		List<CardModel> cards = Owner.Deck.Cards
			.Where(static card => !card.IsBasicStrikeOrDefend && card.Type != CardType.Curse)
			.ToList();
		if (cards.Count == 0)
		{
			return;
		}

		Flash();
		foreach (CardModel card in cards)
		{
			CardModel copy = Owner.RunState.CloneCard(card);
			results.Add(await CardPileCmd.Add(copy, PileType.Deck));
			SaveManager.Instance.MarkCardAsSeen(copy);
		}

		CardCmd.PreviewCardPileAdd(results, 2f);
	}
}
