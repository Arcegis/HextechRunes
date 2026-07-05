namespace HextechRunes;

public sealed class ForbiddenGrimoireRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<ForbiddenGrimoirePower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ForbiddenGrimoirePower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<ForbiddenGrimoirePower>(Owner.Creature, DynamicVars["ForbiddenGrimoirePower"].BaseValue, Owner.Creature, null);
	}
}
