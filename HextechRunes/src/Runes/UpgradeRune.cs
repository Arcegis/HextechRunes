using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace HextechRunes;

public sealed class UpgradeRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override Task AfterObtained()
	{
		if (Owner == null)
		{
			return Task.CompletedTask;
		}

		List<CardModel> cards = Owner.Deck.Cards
			.Where(static card => card.IsUpgradable)
			.ToList();
		if (cards.Count > 0)
		{
			Flash();
			foreach (CardModel card in cards)
			{
				CardCmd.Upgrade(card);
			}
		}

		return Task.CompletedTask;
	}

	public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
		if (player != Owner || cardRewardOptions.Count == 0)
		{
			return false;
		}

		bool modified = false;
		foreach (CardCreationResult result in cardRewardOptions)
		{
			CardModel card = result.Card;
			if (!card.IsUpgradable)
			{
				continue;
			}

			CardCmd.Upgrade(card, CardPreviewStyle.None);
			result.ModifyCard(card, this);
			modified = true;
		}

		return modified;
	}

	public override bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? newCard)
	{
		newCard = null;
		if (card.Owner != Owner || !card.IsUpgradable)
		{
			return false;
		}

		CardCmd.Upgrade(card, CardPreviewStyle.None);
		newCard = card;
		Flash();
		return true;
	}
}
