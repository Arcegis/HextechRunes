namespace HextechRunes;

public sealed class TranscendentEvilRune : HextechSharedCombatVictoryRuneBase
{
	private int _stacks;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedStacks
	{
		get => _stacks;
		set
		{
			_stacks = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => true;

	public override int DisplayAmount => !IsCanonical ? _stacks : 0;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("StacksPerBonus", 4m),
		new PowerVar<FocusPower>(1m),
		new DynamicVar("OrbSlots", 1m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override Task ApplySharedCombatVictory(CombatRoom room)
	{
		if (Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		SavedStacks++;
		Flash(Array.Empty<Creature>());
		return Task.CompletedTask;
	}

	// 额外回合不推进 RoundNumber 且 side turn start 会重入,按 RoundNumber 防重(否则集中+槽位双发)。
	private int _lastProcRound = -1;

	public override Task BeforeCombatStart()
	{
		_lastProcRound = -1;
		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnStart(CombatSide side, HextechCombatState combatState)
	{
		if (side != Owner.Creature.Side
			|| combatState.RoundNumber > 1
			|| !IsDefectOwner
			|| !HextechRoundInterval.TryClaimRound(ref _lastProcRound, combatState.RoundNumber))
		{
			return;
		}

		int bonus = FloorToInt(_stacks / DynamicVars["StacksPerBonus"].BaseValue);
		if (bonus <= 0)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<FocusPower>(Owner.Creature, bonus * DynamicVars["FocusPower"].BaseValue, Owner.Creature, null);
		await OrbCmd.AddSlots(Owner, bonus * DynamicVars["OrbSlots"].IntValue);
	}
}
