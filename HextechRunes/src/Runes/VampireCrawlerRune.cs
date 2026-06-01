using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

public sealed class VampireCrawlerRune : HextechRelicBase
{
	private bool _movedPowerLastPlay;

	public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
		CardModel card,
		bool isAutoPlay,
		ResourceInfo resources,
		PileType pileType,
		CardPilePosition position)
	{
		_movedPowerLastPlay = false;
		if (card.Owner == Owner
			&& !card.IsDupe
			&& card.Type == CardType.Power
			&& pileType == PileType.None)
		{
			_movedPowerLastPlay = true;
			return (PileType.Discard, position);
		}

		return (pileType, position);
	}

	public override Task AfterModifyingCardPlayResultPileOrPosition(CardModel card, PileType pileType, CardPilePosition position)
	{
		if (_movedPowerLastPlay && card.Owner == Owner)
		{
			Flash();
		}

		_movedPowerLastPlay = false;
		return Task.CompletedTask;
	}
}
