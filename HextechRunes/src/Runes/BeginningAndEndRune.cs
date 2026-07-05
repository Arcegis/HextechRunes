namespace HextechRunes;

// 0.8.4 重做:战斗开始时获得 100 层致死性和 6 层倒数计时(原 25 致死性+1 死神形态)。
public sealed class BeginningAndEndRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<LethalityPower>(100m),
		new PowerVar<CountdownPower>(6m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<LethalityPower>(),
		HoverTipFactory.FromPower<CountdownPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<LethalityPower>(Owner.Creature, DynamicVars["LethalityPower"].BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<CountdownPower>(Owner.Creature, DynamicVars["CountdownPower"].BaseValue, Owner.Creature, null);
	}
}
