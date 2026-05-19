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
		new DynamicVar("NormalReplays", 1m),
		new DynamicVar("EliteReplays", 2m),
		new DynamicVar("BossReplays", 2m)
	];

	public override Task BeforeCombatStart()
	{
		ResetTurnState();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetTurnState();
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
		if (_triggeredThisTurn || !IsOwnedAttack(card))
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
		if (!_triggeredThisTurn && IsOwnedAttack(card))
		{
			_triggeredThisTurn = true;
			UpdateTurnStateIdentity();
			Flash();
		}

		return Task.CompletedTask;
	}

	private int GetReplayCount()
	{
		if (Owner?.RunState.CurrentRoom is CombatRoom { RoomType: RoomType.Boss })
		{
			return DynamicVars["BossReplays"].IntValue;
		}

		if (Owner?.RunState.CurrentRoom is CombatRoom { RoomType: RoomType.Elite })
		{
			return DynamicVars["EliteReplays"].IntValue;
		}

		return DynamicVars["NormalReplays"].IntValue;
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
