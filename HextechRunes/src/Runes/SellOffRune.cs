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
	private int _autoPlayTargetsThisCombat;
	private readonly Queue<CardModel> _pendingDiscardedCards = new();

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
		if (!CanAutoPlayDiscardedCard(card))
		{
			return;
		}

		_pendingDiscardedCards.Enqueue(card);
		if (_autoPlaying)
		{
			return;
		}

		_autoPlaying = true;
		try
		{
			while (_pendingDiscardedCards.TryDequeue(out CardModel? discardedCard))
			{
				if (discardedCard == null || !CanAutoPlayDiscardedCard(discardedCard))
				{
					continue;
				}

				await AutoPlayDiscardedCard(choiceContext, discardedCard);
			}
		}
		finally
		{
			_pendingDiscardedCards.Clear();
			_autoPlaying = false;
		}
	}

	private bool CanAutoPlayDiscardedCard(CardModel card)
	{
		return Owner != null
			&& !Owner.Creature.IsDead
			&& card.Owner == Owner
			&& card.Type is CardType.Attack or CardType.Skill or CardType.Power
			&& !card.IsSlyThisTurn;
	}

	private async Task AutoPlayDiscardedCard(PlayerChoiceContext choiceContext, CardModel card)
	{
		Flash();
		card.ExhaustOnNextPlay = true;
		if (card.Pile?.Type != PileType.Hand)
		{
			await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, this, skipVisuals: true);
		}

			HextechCombatState? combatState = Owner!.Creature.CombatState;
			int targetOrdinal = ConsumeCombatProcOrdinal(nameof(SellOffRune), ref _autoPlayTargetsThisCombat);
			Creature? target = RequiresEnemyTarget(card)
				? HextechRuneTargeting.PickRandomHittableEnemy(
					Owner,
					combatState,
					"sell-off",
					combatState?.RoundNumber.ToString() ?? "-1",
					targetOrdinal.ToString(),
					card.Id.Entry)
				: null;
		await HextechAutoPlayHelper.AutoPlayOrMoveToResultPile(choiceContext, card, target);
	}

	private static bool RequiresEnemyTarget(CardModel card)
	{
		return card.TargetType is TargetType.AnyEnemy;
	}
}
