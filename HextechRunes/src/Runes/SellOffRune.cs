using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace HextechRunes;

public sealed class SellOffRune : HextechRelicBase
{
	private bool _autoPlaying;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Sly),
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
	{
		if (_autoPlaying
			|| Owner == null
			|| Owner.Creature.IsDead
			|| card.Owner != Owner
			|| card.Type is not (CardType.Attack or CardType.Skill or CardType.Power)
			|| card.IsSlyThisTurn)
		{
			return;
		}

		_autoPlaying = true;
		try
		{
			Flash();
			card.ExhaustOnNextPlay = true;
			if (card.Pile?.Type != PileType.Hand)
			{
				await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, this, skipVisuals: true);
			}

			HextechCombatState? combatState = Owner.Creature.CombatState;
			Creature? target = RequiresEnemyTarget(card)
				? HextechRuneTargeting.PickRandomHittableEnemy(
					Owner,
					combatState,
					"sell-off",
					combatState?.RoundNumber.ToString() ?? "-1",
					CombatManager.Instance.History.Entries.Count().ToString(),
					card.Id.Entry)
				: null;
			await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default);
		}
		finally
		{
			_autoPlaying = false;
		}
	}

	private static bool RequiresEnemyTarget(CardModel card)
	{
		return card.TargetType is TargetType.AnyEnemy;
	}
}
