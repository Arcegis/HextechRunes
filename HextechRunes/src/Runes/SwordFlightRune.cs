using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

public sealed class SwordFlightRune : HextechRelicBase
{
	private bool _triggeredThisTurn;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<SovereignBlade>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		ResetTriggered(null);
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetTriggered(null);
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			ResetTriggered(combatState);
		}

		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		EnsureTurnScopedStateCurrent(ResetTriggered);
		if (HasTurnProcTriggered(nameof(SwordFlightRune), _triggeredThisTurn)
			|| Owner == null
			|| Owner.Creature.IsDead
			|| !cardPlay.IsFirstInSeries
			|| cardPlay.IsAutoPlay
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not SovereignBlade)
		{
			return;
		}

		int cardsToDraw = Math.Max(0, 10 - PileType.Hand.GetPile(Owner).Cards.Count);
		if (!TryConsumeTurnProc(nameof(SwordFlightRune), ref _triggeredThisTurn))
		{
			return;
		}

		if (cardsToDraw <= 0)
		{
			return;
		}

		Flash();
		await CardPileCmd.Draw(context, cardsToDraw, Owner, fromHandDraw: false);
	}

	private void ResetTriggered()
	{
		ResetTriggered(null);
	}

	private void ResetTriggered(HextechCombatState? combatState)
	{
		_triggeredThisTurn = false;
		UpdateTurnScopedStateIdentity(combatState);
	}
}
