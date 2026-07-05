namespace HextechRunes;

public sealed class InfiniteLoopRune : HextechRelicBase, IHextechSharedCombatVictoryRune
{
	private int _stacks;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCombatVictories
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
		new EnergyVar(1),
		new DynamicVar("StacksPerEnergy", 4m)
	];

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		if (player != Owner)
		{
			return amount;
		}

		return amount + DynamicVars.Energy.BaseValue + FloorToInt(_stacks / DynamicVars["StacksPerEnergy"].BaseValue);
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
		if (Owner != null && !Owner.Creature.IsDead)
		{
			SavedCombatVictories++;
			Flash(Array.Empty<Creature>());
		}

		return Task.CompletedTask;
	}
}
