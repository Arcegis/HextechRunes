namespace HextechRunes;

public sealed class UnmovableMountainRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<BarricadePower>(1m),
		new PowerVar<AfterimagePower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<BarricadePower>(),
		HoverTipFactory.FromPower<AfterimagePower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<BarricadePower>(Owner.Creature, DynamicVars["BarricadePower"].BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<AfterimagePower>(Owner.Creature, DynamicVars["AfterimagePower"].BaseValue, Owner.Creature, null);
	}
}
