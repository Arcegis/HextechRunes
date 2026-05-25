using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

public sealed class AttackDefenseUnityRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<IronWave>()
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		List<CardModel> strikes = Owner.Deck.Cards
			.Where(static card => card.IsBasicStrikeOrDefend && card.Type == CardType.Attack)
			.ToList();
		List<CardModel> defends = Owner.Deck.Cards
			.Where(static card => card.IsBasicStrikeOrDefend && card.Type == CardType.Skill)
			.ToList();
		int pairs = Math.Min(strikes.Count, defends.Count);
		if (pairs <= 0)
		{
			return;
		}

		Flash();
		for (int i = 0; i < pairs; i++)
		{
			await CardPileCmd.RemoveFromDeck(strikes[i], showPreview: false);
			await CardPileCmd.RemoveFromDeck(defends[i], showPreview: false);
		}

		await AddCardCopiesToDeckOrHand<IronWave>(pairs);
	}
}
