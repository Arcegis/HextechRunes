namespace HextechRunes;

public sealed class TriPrismRune : HextechRelicBase
{
	private bool _triggeredThisTurn;

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

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldTrigger(card) || card.EnergyCost.CostsX)
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldTrigger(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		return ShouldTrigger(card) ? playCount + 1 : playCount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (ShouldTrigger(card))
		{
			if (TryConsumeTurnProc(nameof(TriPrismRune), ref _triggeredThisTurn))
			{
				Flash();
			}
		}

		return Task.CompletedTask;
	}

	private bool ShouldTrigger(CardModel card)
	{
		EnsureTurnScopedStateCurrent(ResetTriggered);
		return !HasTurnProcTriggered(nameof(TriPrismRune), _triggeredThisTurn)
			&& card.Owner == Owner
			&& HextechColorlessCardHelper.IsColorlessCard(card);
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
