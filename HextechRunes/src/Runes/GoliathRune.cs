namespace HextechRunes;

public sealed class GoliathRune : HextechRelicBase, IHextechMaxHpScalingRune
{
	private int _baseMaxHp;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedBaseMaxHp
	{
		get => _baseMaxHp;
		set => _baseMaxHp = Math.Max(0, value);
	}

	public int BaseMaxHp
	{
		get => _baseMaxHp;
		set => _baseMaxHp = Math.Max(1, value);
	}

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("HpGainPercent", 0.35m),
		new DynamicVar("DamageMultiplier", 1.2m),
		new DynamicVar("SustainMultiplier", 1.2m),
		new DynamicVar("Scale", 1.35m)
	];

	public decimal MaxHpScale => DynamicVars["Scale"].BaseValue;

	internal float BodyScaleDelta => (float)DynamicVars["Scale"].BaseValue - 1f;

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		// 主符文可能是先获得的另一个系数参与者(星界躯体/百分比锻造器),此时基础值已在它那里,按新乘积重算。
		IHextechMaxHpBaseHolder primary = HextechMaxHpScaling.GetPrimary(Owner) ?? this;
		HextechMaxHpScaling.EnsureBaseInitialized(Owner, primary, assumeAlreadyScaled: false);
		await CreatureCmdCompat.SetMaxHp(Owner.Creature, primary.BaseMaxHp);
		await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp - Owner.Creature.CurrentHp);
		Grow();
	}

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		if (Owner != null && HextechMaxHpScaling.GetPrimary(Owner) is { } primary)
		{
			HextechMaxHpScaling.EnsureBaseInitialized(Owner, primary, assumeAlreadyScaled: true);
		}

		Grow();
		return Task.CompletedTask;
	}

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource) ? DynamicVars["DamageMultiplier"].BaseValue : 1m;
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? DynamicVars["SustainMultiplier"].BaseValue : 1m;
	}

	private void Grow()
	{
		HextechPlayerBodyScaleHelper.Update(Owner);
	}

}
