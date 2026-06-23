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

public sealed class FanTheHammerRune : HextechRelicBase
{
	private bool _triggeredThisTurn;
	private HextechCombatState? _turnStateCombat;
	private int _turnStateRoundNumber = -1;
	private CardModel? _damageReducedCard;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTriggeredThisTurn
	{
		get => false;
		set
		{
			// Legacy save compatibility: this is turn-scoped runtime state and must not enter multiplayer checksums.
			_triggeredThisTurn = false;
			_turnStateCombat = null;
			_turnStateRoundNumber = -1;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Replays", 3m),
		new DynamicVar("DamageMultiplier", 0.35m)
	];

	public override Task BeforeCombatStart()
	{
		ResetTurnState();
		ClearDamageReducedCard();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetTurnState();
		ClearDamageReducedCard();
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			ResetTurnState(combatState);
		}

		return Task.CompletedTask;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (card.Owner != Owner)
		{
			return playCount;
		}

		EnsureTurnStateCurrent();
		if (HasTurnProcTriggered(nameof(FanTheHammerRune), _triggeredThisTurn) || !IsOwnedAttack(card))
		{
			return playCount;
		}

		return playCount + GetReplayCount();
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (card.Owner != Owner)
		{
			return Task.CompletedTask;
		}

		EnsureTurnStateCurrent();
		if (!HasTurnProcTriggered(nameof(FanTheHammerRune), _triggeredThisTurn) && IsOwnedAttack(card))
		{
			if (TryConsumeTurnProc(nameof(FanTheHammerRune), ref _triggeredThisTurn))
			{
				UpdateTurnStateIdentity();
				TrackDamageReducedCard(card);
				Flash();
			}
		}

		return Task.CompletedTask;
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (Owner == null
			|| cardSource is not CardModel card
			|| card != _damageReducedCard
			|| card.Owner != Owner
			|| dealer != Owner.Creature
			|| !IsOwnedAttack(card)
			|| (props & ValueProp.Unpowered) != 0)
		{
			return 1m;
		}

		return DynamicVars["DamageMultiplier"].BaseValue;
	}

	private int GetReplayCount()
	{
		return DynamicVars["Replays"].IntValue;
	}

	private void TrackDamageReducedCard(CardModel card)
	{
		ClearDamageReducedCard();
		_damageReducedCard = card;
		card.Played += ClearDamageReducedCard;
	}

	private void ClearDamageReducedCard()
	{
		if (_damageReducedCard is CardModel card)
		{
			card.Played -= ClearDamageReducedCard;
		}

		_damageReducedCard = null;
	}

	private void ResetTurnState(HextechCombatState? combatState = null)
	{
		_triggeredThisTurn = false;
		UpdateTurnStateIdentity(combatState);
	}

	private void EnsureTurnStateCurrent()
	{
		HextechCombatState? combatState = Owner?.Creature.CombatState;
		if (combatState == null)
		{
			_triggeredThisTurn = false;
			_turnStateCombat = null;
			_turnStateRoundNumber = -1;
			return;
		}

		if (!ReferenceEquals(_turnStateCombat, combatState) || _turnStateRoundNumber != combatState.RoundNumber)
		{
			ResetTurnState(combatState);
		}
	}

	private void UpdateTurnStateIdentity(HextechCombatState? combatState = null)
	{
		combatState ??= Owner?.Creature.CombatState;
		_turnStateCombat = combatState;
		_turnStateRoundNumber = combatState?.RoundNumber ?? -1;
	}
}
