namespace HextechRunes;

public sealed class EasyDoesItRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<MayhemPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<MayhemPower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<MayhemPower>(Owner.Creature, DynamicVars["MayhemPower"].BaseValue, Owner.Creature, null);
	}
}
