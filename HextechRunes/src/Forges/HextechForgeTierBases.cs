namespace HextechRunes;

// 同一效果按白银/黄金/棱彩分档的锻造器共用实现。具体类保留 sealed 类名（即模型 ID）与各档的 CanonicalVars 数值。

public abstract class HextechDamageCoefficientForgeBase : HextechForgeBase, IHextechDamageCoefficientForge
{
	public decimal DamageBonusFractionTotal => StackedMultiplier(DynamicVars["DamageMultiplier"].BaseValue) - 1m;

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource)
			? HextechForgeCoefficientHelper.GetDamageMultiplier(Owner, this)
			: 1m;
	}

	protected static IEnumerable<DynamicVar> CreateDamageVars(decimal damageMultiplier)
	{
		return
		[
			new DynamicVar("DamageMultiplier", damageMultiplier),
			new DynamicVar("DamageBonusPercent", (damageMultiplier - 1m) * 100m)
		];
	}
}

public abstract class HextechSustainCoefficientForgeBase : HextechForgeBase, IHextechSustainCoefficientForge
{
	public decimal SustainBonusFractionTotal => StackedMultiplier(DynamicVars["SustainMultiplier"].BaseValue) - 1m;

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner.Creature
			? HextechForgeCoefficientHelper.GetSustainMultiplier(Owner, this)
			: 1m;
	}

	protected static IEnumerable<DynamicVar> CreateSustainVars(decimal sustainMultiplier)
	{
		return
		[
			new DynamicVar("SustainMultiplier", sustainMultiplier),
			new DynamicVar("SustainBonusPercent", (sustainMultiplier - 1m) * 100m)
		];
	}
}

// 百分比最大生命锻造器。存档属性 SavedBaseMaxHp 必须留在各具体类上声明（SavedProperty 清单按具体类登记），
// 这里只放共享的基础值与重算逻辑。
public abstract class HextechPercentHpForgeBase : HextechForgeBase, IHextechPercentHpForge
{
	private int _baseMaxHp;

	public int BaseMaxHp
	{
		get => _baseMaxHp;
		set => _baseMaxHp = Math.Max(1, value);
	}

	public decimal MaxHpPercentTotal => DynamicVars["MaxHpPercent"].BaseValue * StackAmount;

	public override bool HasUponPickupEffect => true;

	// 读档时 0 表示尚未记录基础值，所以存档入口的下限是 0，运行时入口的下限是 1。
	protected int SavedBaseMaxHpValue
	{
		get => _baseMaxHp;
		set => _baseMaxHp = Math.Max(0, value);
	}

	public override async Task AfterObtained()
	{
		Flash();
		await HextechMaxHpScaling.ReapplyScale(Owner);
	}

	protected static IEnumerable<DynamicVar> CreatePercentVars(decimal maxHpPercent)
	{
		return
		[
			new DynamicVar("MaxHpPercent", maxHpPercent)
		];
	}
}

public abstract class HextechUpgradeForgeBase : HextechForgeBase
{
	public override bool HasUponPickupEffect => true;

	// 各档沿用原有的随机盐字符串；改动会改变同一局里被升级的牌。
	protected abstract string UpgradeRandomSalt { get; }

	public override Task AfterObtained()
	{
		List<CardModel> cards = HextechStableRandom.PickDistinct(
			Owner.Deck.Cards
				.Where(static card => card.IsUpgradable)
				.ToList(),
			DynamicVars.Cards.IntValue,
			(RunState)Owner.RunState,
			HextechStableRandom.CardKey,
			UpgradeRandomSalt,
			HextechStableRandom.PlayerKey(Owner),
			Owner.Deck.Cards.Count.ToString());
		if (cards.Count == 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		foreach (CardModel card in cards)
		{
			CardCmd.Upgrade(card);
		}

		return Task.CompletedTask;
	}
}

public abstract class HextechFocusForgeBase : HextechForgeBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<FocusPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		return IsDefectOwner
			? ApplyStackedPowerAtCombatStart<FocusPower>(DynamicVars["FocusPower"].BaseValue)
			: Task.CompletedTask;
	}
}
