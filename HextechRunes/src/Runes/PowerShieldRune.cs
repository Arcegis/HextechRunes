namespace HextechRunes;

public sealed class PowerShieldRune : HextechRelicBase
{
	private bool _triggeredThisTurn;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

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

	public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
		EnsureTurnScopedStateCurrent(ResetTurnState);
		if (HasTurnProcTriggered(nameof(PowerShieldRune), _triggeredThisTurn) || Owner == null || creature != Owner.Creature || amount <= 0m)
		{
			return;
		}

		if (!TryConsumeTurnProc(nameof(PowerShieldRune), ref _triggeredThisTurn))
		{
			return;
		}

		Flash();
		int strength = GetPlayerActNumberForScaling();
		await PowerCmd.Apply<HextechPowerShieldTemporaryStrengthPower>(Owner.Creature, strength, Owner.Creature, cardSource);
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
