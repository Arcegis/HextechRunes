using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HextechRunes;

public sealed class BreadSandwichRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Replays", 1m)
	];

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		return card.Owner == Owner ? playCount + DynamicVars["Replays"].IntValue : playCount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (card.Owner == Owner)
		{
			Flash();
		}

		return Task.CompletedTask;
	}
}
