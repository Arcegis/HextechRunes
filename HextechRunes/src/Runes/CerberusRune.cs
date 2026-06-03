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

public sealed class CerberusRune : HextechRelicBase
{
	private int _attacksPlayedThisTurn;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedAttacksPlayedThisTurn
	{
		get
		{
			EnsureTurnScopedStateCurrent(ResetAttacksPlayedThisTurn);
			return GetTurnProcCount(nameof(CerberusRune), _attacksPlayedThisTurn);
		}
		set
		{
			_attacksPlayedThisTurn = Math.Max(0, value);
			InvokeDisplayAmountChanged();
			UpdateTurnScopedStateIdentity();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount => !IsCanonical ? Math.Max(0, DynamicVars["FreeAttacks"].IntValue - GetTurnProcCount(nameof(CerberusRune), _attacksPlayedThisTurn)) : 0;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("FreeAttacks", 3m)
	];

	public override Task BeforeCombatStart()
	{
		ResetAttacksPlayedThisTurn(null);
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetAttacksPlayedThisTurn(null);
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			ResetAttacksPlayedThisTurn(combatState);
		}

		return Task.CompletedTask;
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldPlayAttackForFree(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldPlayAttackForFree(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		EnsureTurnScopedStateCurrent(ResetAttacksPlayedThisTurn);
		if (!cardPlay.IsFirstInSeries || cardPlay.IsAutoPlay || !IsOwnedAttack(cardPlay.Card))
		{
			return Task.CompletedTask;
		}

		if (!TryConsumeTurnProc(nameof(CerberusRune), ref _attacksPlayedThisTurn, int.MaxValue))
		{
			return Task.CompletedTask;
		}

		if (GetTurnProcCount(nameof(CerberusRune), _attacksPlayedThisTurn) <= DynamicVars["FreeAttacks"].IntValue)
		{
			Flash();
		}

		return Task.CompletedTask;
	}

	private bool ShouldPlayAttackForFree(CardModel card)
	{
		EnsureTurnScopedStateCurrent(ResetAttacksPlayedThisTurn);
		return Owner != null
			&& card.Owner == Owner
			&& IsOwnedAttack(card)
			&& card.Pile?.Type == PileType.Hand
			&& !card.EnergyCost.CostsX
			&& !HasTurnProcReachedLimit(nameof(CerberusRune), _attacksPlayedThisTurn, DynamicVars["FreeAttacks"].IntValue);
	}

	private void ResetAttacksPlayedThisTurn()
	{
		ResetAttacksPlayedThisTurn(null);
	}

	private void ResetAttacksPlayedThisTurn(HextechCombatState? combatState)
	{
		_attacksPlayedThisTurn = 0;
		InvokeDisplayAmountChanged();
		UpdateTurnScopedStateIdentity(combatState);
	}
}
