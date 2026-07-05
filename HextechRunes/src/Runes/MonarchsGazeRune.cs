namespace HextechRunes;

public sealed class MonarchsGazeRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<MonarchsGazePower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<MonarchsGazePower>()
	];

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<MonarchsGazePower>(Owner.Creature, DynamicVars["MonarchsGazePower"].BaseValue, Owner.Creature, null);
	}
}
