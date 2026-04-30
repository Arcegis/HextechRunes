using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal static class HextechCardGeneration
{
	internal static async Task<IReadOnlyList<CardPileAddResult>> AddGeneratedCardsToCombat(
		IEnumerable<CardModel> cards,
		PileType pileType,
		bool addedByPlayer,
		CardPilePosition position = CardPilePosition.Bottom,
		bool previewNonHandAdds = true)
	{
		List<CardModel> cardList = cards.ToList();
		if (cardList.Count == 0)
		{
			return Array.Empty<CardPileAddResult>();
		}

		IReadOnlyList<CardPileAddResult> results = await CardPileCmd.AddGeneratedCardsToCombat(
			cardList,
			pileType,
			addedByPlayer,
			position);
		foreach (CardModel card in cardList)
		{
			SaveManager.Instance.MarkCardAsSeen(card);
		}

		if (previewNonHandAdds && pileType != PileType.Hand && results.Count > 0)
		{
			CardCmd.PreviewCardPileAdd(results);
		}

		return results;
	}

	internal static async Task<CardPileAddResult?> AddGeneratedCardToCombat(
		CardModel card,
		PileType pileType,
		bool addedByPlayer,
		CardPilePosition position = CardPilePosition.Bottom,
		bool previewNonHandAdds = true)
	{
		IReadOnlyList<CardPileAddResult> results = await AddGeneratedCardsToCombat(
			[card],
			pileType,
			addedByPlayer,
			position,
			previewNonHandAdds);
		return results.Count > 0 ? results[0] : null;
	}
}
