namespace HextechRunes;

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
