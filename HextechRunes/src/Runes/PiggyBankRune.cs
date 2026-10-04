namespace HextechRunes;

public sealed class PiggyBankRune : HextechRelicBase
{
	private bool _grantingGold;

	// 旧版本存档兼容占位：原为待发放的战后金币计数，金币已改为触发时立即发放；名称与类型须保留。
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCounter
	{
		get => 0;
		set { }
	}

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new GoldVar(200),
		new DynamicVar("CounterGain", 20m)
	];

	public override Task AfterObtained()
	{
		return GrantGold(DynamicVars.Gold.BaseValue);
	}

	public override Task AfterDamageReceived(
		PlayerChoiceContext choiceContext,
		Creature target,
		DamageResult result,
		ValueProp props,
		Creature? dealer,
		CardModel? cardSource)
	{
		if (_grantingGold
			|| target != Owner.Creature
			|| result.UnblockedDamage <= 0m)
		{
			return Task.CompletedTask;
		}

		Flash();
		return GrantGold(DynamicVars["CounterGain"].IntValue);
	}

	private async Task GrantGold(decimal amount)
	{
		// 鲜血神像会在获得金币时造成伤害；拾取奖励同样必须覆盖这条反馈链。
		_grantingGold = true;
		try
		{
			await PlayerCmd.GainGold(amount, Owner);
		}
		finally
		{
			_grantingGold = false;
		}
	}
}
