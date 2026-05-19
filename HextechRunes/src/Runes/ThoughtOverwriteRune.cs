using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class ThoughtOverwriteRune : HextechRelicBase
{
	internal const string EtherealMarkerSavedPropertyName = "SavedThoughtOverwriteEtherealMarker";

	public override bool HasUponPickupEffect => true;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	private int SavedThoughtOverwriteEtherealMarker
	{
		get => 0;
		set { }
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Replays", 1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task AfterObtained()
	{
		if (Owner == null || !IsNecrobinderPlayer(Owner))
		{
			return;
		}

		List<CardModel> selectable = Owner.Deck.Cards.ToList();
		if (selectable.Count == 0)
		{
			return;
		}

		IEnumerable<CardModel> selected = await CardSelectCmd.FromDeckGeneric(
			Owner,
			new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 0, selectable.Count)
			{
				Cancelable = true,
				RequireManualConfirmation = true
			});

		List<CardModel> selectedCards = selected.ToList();
		if (selectedCards.Count == 0)
		{
			return;
		}

		Flash();
		foreach (CardModel card in selectedCards)
		{
			ThoughtOverwriteKeywordPersistence.Track(card);
			CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
		}
	}

	public override Task AfterCardEnteredCombat(CardModel card)
	{
		if (Owner == null || card.Owner != Owner)
		{
			return Task.CompletedTask;
		}

		if (ThoughtOverwriteKeywordPersistence.IsTracked(card.DeckVersion))
		{
			ThoughtOverwriteKeywordPersistence.Restore(card);
		}

		return Task.CompletedTask;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (Owner == null || card.Owner != Owner || !card.Keywords.Contains(CardKeyword.Ethereal))
		{
			return playCount;
		}

		return playCount + DynamicVars["Replays"].IntValue;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (Owner != null && card.Owner == Owner && card.Keywords.Contains(CardKeyword.Ethereal))
		{
			Flash();
		}

		return Task.CompletedTask;
	}
}
