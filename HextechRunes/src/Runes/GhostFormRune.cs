namespace HextechRunes;

public sealed class GhostFormRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<IntangiblePower>(3m),
		new PowerVar<NoBlockPower>(3m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<IntangiblePower>(),
		HoverTipFactory.FromPower<NoBlockPower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<IntangiblePower>(Owner.Creature, DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, null);
		await PowerCmd.Apply<NoBlockPower>(Owner.Creature, DynamicVars["NoBlockPower"].BaseValue, Owner.Creature, null);
	}
}
