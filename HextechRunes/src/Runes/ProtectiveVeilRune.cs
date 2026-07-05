namespace HextechRunes;

public sealed class ProtectiveVeilRune : HextechRelicBase
{
	private int _turnsThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedTurnsThisCombat
	{
		get => _turnsThisCombat;
		set => _turnsThisCombat = Math.Max(0, value);
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<ArtifactPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ArtifactPower>()
	];

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is not CombatRoom || Owner == null)
		{
			return Task.CompletedTask;
		}

		_turnsThisCombat = 0;
		Flash();
		return PowerCmd.Apply<ArtifactPower>(Owner.Creature, DynamicVars["ArtifactPower"].BaseValue, Owner.Creature, null);
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_turnsThisCombat = 0;
		return Task.CompletedTask;
	}

	public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner == null || side != Owner.Creature.Side)
		{
			return;
		}

		_turnsThisCombat++;
		if (_turnsThisCombat % 2 != 0)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<ArtifactPower>(Owner.Creature, DynamicVars["ArtifactPower"].BaseValue, Owner.Creature, null);
	}
}
