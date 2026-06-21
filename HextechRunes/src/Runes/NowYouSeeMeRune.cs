using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace HextechRunes;

public sealed class NowYouSeeMeRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| !IsOwnedCard(card)
			|| card.Type is not (CardType.Status or CardType.Curse)
			|| card.Pile?.Type != PileType.Discard
			|| card.IsSlyThisTurn
			|| CombatManager.Instance.IsOverOrEnding
			|| !HextechSts2Compat.IsPartOfPlayerTurn(Owner))
		{
			return;
		}

		Flash();
		await CardCmd.Exhaust(choiceContext, card, causedByEthereal: false, skipVisuals: true);
	}
}
