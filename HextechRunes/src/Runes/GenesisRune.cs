using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

public sealed class GenesisRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		List<CardModel> cardsToRemove = Owner.Deck.Cards
			.Where(static card => card.Rarity != CardRarity.Basic)
			.ToList();
		if (cardsToRemove.Count == 0)
		{
			return;
		}

		Flash();
		foreach (CardModel card in cardsToRemove)
		{
			await CardPileCmd.RemoveFromDeck(card, showPreview: false);
		}

		CardCreationOptions options = new(
			Owner.Character.CardPool.AllCards,
			CardCreationSource.Encounter,
			CardRarityOddsType.RegularEncounter);
		List<Reward> rewards = Enumerable.Range(0, cardsToRemove.Count)
			.Select(_ => (Reward)new CardReward(options, 3, Owner))
			.ToList();
		await RewardsCmd.OfferCustom(Owner, rewards);
	}
}
