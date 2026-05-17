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
using MegaCrit.Sts2.Core.ValueProps;

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
