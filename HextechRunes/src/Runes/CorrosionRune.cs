namespace HextechRunes;

public sealed class CorrosionRune : HextechRelicBase
{
	private bool _triggeredThisTurn;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(1m),
		new PowerVar<DexterityPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>()
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

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		EnsureTurnScopedStateCurrent(ResetTurnState);
		if (HasTurnProcTriggered(nameof(CorrosionRune), _triggeredThisTurn)
			|| Owner == null
			|| target.Side != CombatSide.Enemy
			|| !target.IsAlive
			|| result.TotalDamage <= 0m
			|| !IsDamageFromOwner(dealer, cardSource))
		{
			return;
		}

		if (!TryConsumeTurnProc(nameof(CorrosionRune), ref _triggeredThisTurn))
		{
			return;
		}

		Flash([target]);
		await PowerCmd.Apply<StrengthPower>(target, -DynamicVars.Strength.BaseValue, Owner.Creature, cardSource);
		await PowerCmd.Apply<DexterityPower>(target, -DynamicVars.Dexterity.BaseValue, Owner.Creature, cardSource);
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
