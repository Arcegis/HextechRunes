namespace HextechRunes;

public sealed class SpeedDemonRune : HextechRelicBase
{
	private bool _triggeredThisTurn;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTriggeredThisTurn
	{
		get => false;
		set
		{
			// Legacy save compatibility: this is turn-scoped runtime state and must not enter multiplayer checksums.
			_triggeredThisTurn = false;
			UpdateTurnScopedStateIdentity(null);
		}
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

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		EnsureTurnScopedStateCurrent(ResetTurnState);
		if (HasTurnProcTriggered(nameof(SpeedDemonRune), _triggeredThisTurn)
			|| Owner == null
			|| target.Side != CombatSide.Enemy
			|| result.UnblockedDamage <= 0
			|| (!IsOwnerOrPet(dealer) && cardSource?.Owner != Owner))
		{
			return;
		}

		if (!TryConsumeTurnProc(nameof(SpeedDemonRune), ref _triggeredThisTurn))
		{
			return;
		}

		Flash([target]);
		await CardPileCmd.Draw(choiceContext, 2m, Owner);
	}

	private void ResetTurnState()
	{
		ResetTurnState(null);
	}

	private void ResetTurnState(HextechCombatState? combatState)
	{
		_triggeredThisTurn = false;
		UpdateTurnScopedStateIdentity(combatState);
	}
}
