using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

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
