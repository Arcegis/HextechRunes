namespace HextechRunes;

public sealed class NineDragonPowerRune : HextechRelicBase, IHextechMaxHpScalingRune, IHextechHealingMultiplierProvider
{
	private int _baseMaxHp;
	private int _stacks;

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

	public override int DisplayAmount => _stacks;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<RegenPower>(1m),
		new DynamicVar("StackBonusPercent", 3m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<RegenPower>()
	];

	public decimal SustainMultiplier => 1m + _stacks * DynamicVars["StackBonusPercent"].BaseValue / 100m;

	public decimal MaxHpScale => SustainMultiplier;

	internal float BodyScaleDelta => _stacks * (float)(DynamicVars["StackBonusPercent"].BaseValue / 100m);

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		HextechMaxHpScaling.EnsureScaledBaseInitialized(Owner);

		Grow();
		return Task.CompletedTask;
	}

	public override Task BeforeCombatStart()
	{
		if (Owner.Creature.IsDead || _stacks <= 0)
		{
			return Task.CompletedTask;
		}

		return PowerCmd.Apply<RegenPower>(Owner.Creature, _stacks * DynamicVars["RegenPower"].BaseValue, Owner.Creature, null);
	}

	public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
	{
		if (Owner.Creature.IsDead || !IsPotionUseOwnedByOrTargetingOwner(potion, target))
		{
			return;
		}

		HextechMaxHpScaling.EnsureScaledBaseInitialized(Owner, this);
		SavedStacks++;
		Flash();
		await HextechMaxHpScaling.ReapplyScale(Owner);
		Grow();
	}

	decimal IHextechHealingMultiplierProvider.ModifyHealingMultiplicative(Player player, Creature creature, decimal amount)
	{
		return IsFirstOwnedInstance(player) ? SustainMultiplier : 1m;
	}

	private void Grow()
	{
		HextechPlayerBodyScaleHelper.Update(Owner);
	}
}
