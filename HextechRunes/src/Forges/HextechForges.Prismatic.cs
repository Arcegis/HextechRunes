namespace HextechRunes;

public sealed class PrismaticLifeForge : HextechForgeBase, IHextechPercentHpForge
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

	public decimal MaxHpPercentTotal => DynamicVars["MaxHpPercent"].BaseValue * StackAmount;

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("MaxHpPercent", 30m)
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Flash();
		await HextechMaxHpScaling.ReapplyScale(Owner);
	}
}

public sealed class AttackForge : HextechForgeBase, IHextechDamageCoefficientForge
{
	private const decimal DamageMultiplierValue = 1.2m;
	private const decimal DamageBonusPercentValue = (DamageMultiplierValue - 1m) * 100m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DamageMultiplier", DamageMultiplierValue),
		new DynamicVar("DamageBonusPercent", DamageBonusPercentValue)
	];

	public decimal DamageBonusFractionTotal => StackedMultiplier(DynamicVars["DamageMultiplier"].BaseValue) - 1m;

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return Owner != null && IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource)
			? HextechForgeCoefficientHelper.GetDamageMultiplier(Owner, this)
			: 1m;
	}
}

public sealed class ProtectionForge : HextechForgeBase, IHextechSustainCoefficientForge
{
	private const decimal SustainMultiplierValue = 1.2m;
	private const decimal SustainBonusPercentValue = (SustainMultiplierValue - 1m) * 100m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("SustainMultiplier", SustainMultiplierValue),
		new DynamicVar("SustainBonusPercent", SustainBonusPercentValue)
	];

	public decimal SustainBonusFractionTotal => StackedMultiplier(DynamicVars["SustainMultiplier"].BaseValue) - 1m;

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return Owner != null && target == Owner.Creature
			? HextechForgeCoefficientHelper.GetSustainMultiplier(Owner, this)
			: 1m;
	}
}

public sealed class EnergyForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1)
	];

	public override Task AfterEnergyResetLate(Player player)
	{
		if (Owner == null || player != Owner || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PlayerCmd.GainEnergy(Stacked(DynamicVars.Energy.BaseValue), Owner);
	}
}

public sealed class RitualForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<RitualPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<RitualPower>()
	];

	public override Task BeforeCombatStart()
	{
		return ApplyStackedPowerAtCombatStart<RitualPower>(DynamicVars["RitualPower"].BaseValue);
	}
}

public sealed class RegenForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<RegenPower>(4m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<RegenPower>()
	];

	public override Task BeforeCombatStart()
	{
		return ApplyStackedPowerAtCombatStart<RegenPower>(DynamicVars["RegenPower"].BaseValue);
	}
}

public sealed class BufferForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<BufferPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<BufferPower>()
	];

	public override Task BeforeCombatStart()
	{
		return ApplyStackedPowerAtCombatStart<BufferPower>(DynamicVars["BufferPower"].BaseValue);
	}
}

public sealed class SlipperyForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<SlipperyPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<SlipperyPower>()
	];

	public override Task BeforeCombatStart()
	{
		return ApplyStackedPowerAtCombatStart<SlipperyPower>(DynamicVars["SlipperyPower"].BaseValue);
	}
}

public sealed class PrismaticArtifactForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<ArtifactPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ArtifactPower>()
	];

	public override Task BeforeCombatStart()
	{
		return ApplyStackedPowerAtCombatStart<ArtifactPower>(DynamicVars["ArtifactPower"].BaseValue);
	}
}

public sealed class FortuneForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new GoldVar(100)
	];

	internal int ExtraGoldRewardAmount => FloorToInt(Stacked(DynamicVars.Gold.BaseValue));

	public override Task AfterCombatEnd(CombatRoom room)
	{
		if (Owner == null)
		{
			return Task.CompletedTask;
		}

		Flash();
		HextechGoldRewardHelper.AddFixedExtraGoldReward(
			room,
			Owner,
			ExtraGoldRewardAmount);
		return Task.CompletedTask;
	}
}

public sealed class VoidForge : HextechForgeBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<VoidFormPower>(1m)
	];

	public override Task BeforeCombatStart()
	{
		return ApplyStackedPowerAtCombatStart<VoidFormPower>(DynamicVars["VoidFormPower"].BaseValue);
	}
}
