namespace HextechRunes;

public sealed class WarmupExerciseRune : HextechRelicBase
{
	private bool _dealtDamageThisTurn;
	private bool _triggeredThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTriggeredThisCombat
	{
		get => _triggeredThisCombat;
		set => _triggeredThisCombat = value;
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(2m),
		new PowerVar<DexterityPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>()
	];

	public override Task BeforeCombatStart()
	{
		_dealtDamageThisTurn = false;
		_triggeredThisCombat = false;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_dealtDamageThisTurn = false;
		_triggeredThisCombat = false;
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner != null && side == Owner.Creature.Side)
		{
			_dealtDamageThisTurn = false;
		}

		return Task.CompletedTask;
	}

	public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (Owner != null && target.Side == CombatSide.Enemy && result.UnblockedDamage > 0 && IsDamageFromOwner(dealer, cardSource))
		{
			TryConsumeTurnProc($"{nameof(WarmupExerciseRune)}:Damage", ref _dealtDamageThisTurn);
		}

		return Task.CompletedTask;
	}

	public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| side != Owner.Creature.Side
			|| HasTurnProcTriggered($"{nameof(WarmupExerciseRune)}:Damage", _dealtDamageThisTurn)
			|| _triggeredThisCombat)
		{
			return;
		}

		_triggeredThisCombat = true;
		Flash();
		await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<DexterityPower>(Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, null);
	}
}
