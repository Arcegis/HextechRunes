namespace HextechRunes;

public sealed class OldIdolRune : HextechRelicBase
{
	private bool _secondTurnStrengthGranted;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedSecondTurnStrengthGranted
	{
		get => _secondTurnStrengthGranted;
		set => _secondTurnStrengthGranted = value;
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(10m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override Task BeforeCombatStart()
	{
		_secondTurnStrengthGranted = false;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_secondTurnStrengthGranted = false;
		return Task.CompletedTask;
	}

	public override decimal ModifyHandDrawLate(Player player, decimal count)
	{
		return player == Owner && player.Creature.CombatState?.RoundNumber == 1 ? 0m : count;
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || _secondTurnStrengthGranted || combatState.RoundNumber != 2)
		{
			return;
		}

		_secondTurnStrengthGranted = true;
		Flash();
		await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, null);
	}
}
