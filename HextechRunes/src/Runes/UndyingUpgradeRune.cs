using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HextechRunes;

public sealed class UndyingUpgradeRune : CardUpgradeRuneBase<Undeath>
{
	internal const string EtherealMarkerSavedPropertyName = "SavedUndyingUpgradeEtherealMarker";

	public override bool HasUponPickupEffect => true;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	private int SavedUndyingUpgradeEtherealMarker
	{
		get => 0;
		set { }
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Undeath>(),
		HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
	];

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task AfterObtained()
	{
		if (Owner == null)
		{
			return Task.CompletedTask;
		}

		List<CardModel> undeaths = Owner.Deck.Cards
			.Where(static card => card is Undeath)
			.ToList();
		if (undeaths.Count == 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		foreach (CardModel card in undeaths)
		{
			ApplyPersistentEthereal(card);
		}

		return Task.CompletedTask;
	}

	public override Task AfterCardEnteredCombat(CardModel card)
	{
		if (Owner == null || card.Owner != Owner)
		{
			return Task.CompletedTask;
		}

		if (UndyingEtherealKeywordPersistence.IsTracked(card.DeckVersion))
		{
			UndyingEtherealKeywordPersistence.Restore(card);
		}

		return Task.CompletedTask;
	}

	public override bool TryModifyCardBeingAddedToDeck(CardModel card, out CardModel? newCard)
	{
		newCard = null;
		if (card.Owner != Owner || card is not Undeath)
		{
			return false;
		}

		ApplyPersistentEthereal(card);
		newCard = card;
		Flash();
		return true;
	}

	private static void ApplyPersistentEthereal(CardModel card)
	{
		UndyingEtherealKeywordPersistence.Track(card);
		CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
	}
}
