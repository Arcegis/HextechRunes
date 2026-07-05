namespace HextechRunes;

public sealed class TankEngineRune : HextechRelicBase, IHextechSharedCombatVictoryRune
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
		new DynamicVar("HpGainPercent", 0.06m),
		new DynamicVar("ScalePercent", 6m)
	];

	internal float BodyScaleDelta => _stacks * (float)(DynamicVars["ScalePercent"].BaseValue / 100m);

	public override Task AfterObtained()
	{
		Grow();
		return Task.CompletedTask;
	}

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		Grow();
		return Task.CompletedTask;
	}

	public override async Task AfterCombatVictory(CombatRoom room)
	{
		if (IsNetworkMultiplayer())
		{
			return;
		}

		await ApplySharedCombatVictory(room);
	}

	public async Task ApplySharedCombatVictory(CombatRoom room)
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		SavedStacks++;
		Flash(Array.Empty<Creature>());
		int hpGain = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * DynamicVars["HpGainPercent"].BaseValue));
		await CreatureCmd.GainMaxHp(Owner.Creature, hpGain);
		Grow();
	}

	private void Grow()
	{
		HextechPlayerBodyScaleHelper.Update(Owner);
	}
}
