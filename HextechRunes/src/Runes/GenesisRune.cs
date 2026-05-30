using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

public sealed class GenesisRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override async Task AfterObtained()
	{
		Player? player = Owner;
		if (player == null)
		{
			return;
		}

		List<CardModel> cardsToRemove = player.Deck.Cards.ToList();
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
			new[] { player.Character.CardPool },
			CardCreationSource.Other,
			CardRarityOddsType.RegularEncounter);
		List<Reward> rewards = Enumerable.Range(0, cardsToRemove.Count)
			.Select(_ => (Reward)new GenesisUpgradedCardReward(options, 3, player))
			.ToList();
		await RewardsCmd.OfferCustom(player, rewards);
	}
}
