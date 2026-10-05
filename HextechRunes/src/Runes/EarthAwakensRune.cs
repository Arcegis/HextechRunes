namespace HextechRunes;

public sealed class EarthAwakensRune : HextechRelicBase
{
	// 旧版本存档兼容占位：原为"本场已施加首层"标记，已不再使用；名称与类型须保留。
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedInitialPowerAppliedThisCombat
	{
		get => false;
		set { }
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<RollingBoulderPower>(5m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<RollingBoulderPower>()
	];

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != Owner || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<RollingBoulderPower>(Owner.Creature, DynamicVars["RollingBoulderPower"].BaseValue, Owner.Creature, null);
	}
}
