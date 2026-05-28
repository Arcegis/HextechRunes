using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace HextechRunes;

public abstract class FirstTypedCardReplayRuneBase : HextechRelicBase
{
	private bool _triggeredThisTurn;

	protected abstract CardType TargetCardType { get; }

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Replays", 1m)
	];

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

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		EnsureTurnScopedStateCurrent(ResetTriggered);
		if (_triggeredThisTurn || !IsOwnedTargetType(card))
		{
			return playCount;
		}

		return playCount + DynamicVars["Replays"].IntValue;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		EnsureTurnScopedStateCurrent(ResetTriggered);
		if (!_triggeredThisTurn && IsOwnedTargetType(card))
		{
			_triggeredThisTurn = true;
			UpdateTurnScopedStateIdentity();
			Flash();
		}

		return Task.CompletedTask;
	}

	private bool IsOwnedTargetType(CardModel? card)
	{
		if (TargetCardType == CardType.Skill)
		{
			return IsOwnedSkill(card);
		}

		return card?.Owner == Owner && card.Type == TargetCardType;
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
