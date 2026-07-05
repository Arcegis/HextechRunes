namespace HextechRunes;

public sealed class LubricantRune : HextechRelicBase
{
	private bool _usedThisTurn;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedUsedThisTurn
	{
		get
		{
			EnsureTurnScopedStateCurrent(ResetTurnState);
			return HasTurnProcTriggered(nameof(LubricantRune), _usedThisTurn);
		}
		set
		{
			_usedThisTurn = value;
			InvokeDisplayAmountChanged();
			UpdateTurnScopedStateIdentity();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount => !IsCanonical && !HasTurnProcTriggered(nameof(LubricantRune), _usedThisTurn) ? 1 : 0;

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		ResetTurnState(null);
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetTurnState(null);
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

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldPowerCardBeFree(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldPowerCardBeFree(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		EnsureTurnScopedStateCurrent(ResetTurnState);
		if (HasTurnProcTriggered(nameof(LubricantRune), _usedThisTurn)
			|| cardPlay.IsAutoPlay
			|| !cardPlay.IsFirstInSeries
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card.Type != CardType.Power)
		{
			return Task.CompletedTask;
		}

		if (!TryConsumeTurnProc(nameof(LubricantRune), ref _usedThisTurn))
		{
			return Task.CompletedTask;
		}

		Flash();
		return Task.CompletedTask;
	}

	private bool ShouldPowerCardBeFree(CardModel card)
	{
		EnsureTurnScopedStateCurrent(ResetTurnState);
		return !HasTurnProcTriggered(nameof(LubricantRune), _usedThisTurn)
			&& Owner != null
			&& card.Owner == Owner
			&& card.Type == CardType.Power
			&& card.Pile?.Type is PileType.Hand or PileType.Play;
	}

	private void ResetTurnState()
	{
		ResetTurnState(null);
	}

	private void ResetTurnState(HextechCombatState? combatState)
	{
		_usedThisTurn = false;
		InvokeDisplayAmountChanged();
		UpdateTurnScopedStateIdentity(combatState);
	}
}
