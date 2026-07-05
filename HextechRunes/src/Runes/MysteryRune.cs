namespace HextechRunes;

public sealed class MysteryRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<MayhemPower>(2m),
		new PowerVar<EntropyPower>(2m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<MayhemPower>(),
		HoverTipFactory.FromPower<EntropyPower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<MayhemPower>(Owner.Creature, DynamicVars["MayhemPower"].BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<EntropyPower>(Owner.Creature, DynamicVars["EntropyPower"].BaseValue, Owner.Creature, null);
	}
}
