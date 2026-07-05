namespace HextechRunes;

public sealed class BloodPactRune : HextechRelicBase
{
	private int _pendingTemporaryStrength;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	private int SavedPendingTemporaryStrength
	{
		get => _pendingTemporaryStrength;
		set => _pendingTemporaryStrength = Math.Max(0, value);
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		_pendingTemporaryStrength = 0;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_pendingTemporaryStrength = 0;
		return Task.CompletedTask;
	}

	public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner == null || side != Owner.Creature.Side || Owner.Creature.IsDead || _pendingTemporaryStrength <= 0)
		{
			return;
		}

		decimal strength = _pendingTemporaryStrength * DynamicVars.Strength.BaseValue;
		_pendingTemporaryStrength = 0;
		Flash();
		await PowerCmd.Apply<HextechBloodPactTemporaryStrengthPower>(Owner.Creature, strength, Owner.Creature, null);
	}

	public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (Owner == null
			|| creature != Owner.Creature
			|| delta >= 0m
			|| Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		_pendingTemporaryStrength++;
		return Task.CompletedTask;
	}
}
