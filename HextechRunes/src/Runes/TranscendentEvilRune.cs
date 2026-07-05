namespace HextechRunes;

public sealed class TranscendentEvilRune : HextechRelicBase, IHextechSharedCombatVictoryRune
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

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (IsNetworkMultiplayer())
		{
			return Task.CompletedTask;
		}

		return ApplySharedCombatVictory(room);
	}

	public Task ApplySharedCombatVictory(CombatRoom room)
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		SavedStacks++;
		Flash(Array.Empty<Creature>());
		return Task.CompletedTask;
	}

	public override async Task AfterSideTurnStart(CombatSide side, HextechCombatState combatState)
	{
		if (Owner == null || side != Owner.Creature.Side || combatState.RoundNumber > 1 || !IsDefectOwner)
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
