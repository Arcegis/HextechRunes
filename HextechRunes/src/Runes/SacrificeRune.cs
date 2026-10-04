namespace HextechRunes;

public sealed class SacrificeRune : HextechRelicBase, IHextechHealingMultiplierProvider
{
	private const decimal SustainMultiplierValue = 1.1m;
	private const decimal SustainBonusPercentValue = (SustainMultiplierValue - 1m) * 100m;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CountPerEnemy", 5m),
		new DynamicVar("SustainMultiplier", SustainMultiplierValue),
		new DynamicVar("SustainBonusPercent", SustainBonusPercentValue)
	];

	public decimal SustainMultiplier => DynamicVars["SustainMultiplier"].BaseValue;

	// 旧版本存档兼容占位：原为待发放的战后金币计数，金币已改为触发时立即发放；名称与类型须保留。
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCountThisCombat
	{
		get => 0;
		set { }
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || player.Creature.CombatState == null)
		{
			return Task.CompletedTask;
		}

		int gold = player.Creature.CombatState.Enemies.Count(static enemy => enemy.IsAlive && enemy.Side == CombatSide.Enemy) * DynamicVars["CountPerEnemy"].IntValue;
		return gold > 0 ? PlayerCmd.GainGold(gold, player) : Task.CompletedTask;
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner.Creature ? SustainMultiplier : 1m;
	}

	decimal IHextechHealingMultiplierProvider.ModifyHealingMultiplicative(Player player, Creature creature, decimal amount)
	{
		return IsFirstOwnedInstance(player) ? SustainMultiplier : 1m;
	}
}
