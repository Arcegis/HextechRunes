namespace HextechRunes;

public sealed class HardBonesRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<CalcifyPower>(8m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<CalcifyPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead || !IsNecrobinderPlayer(Owner))
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<CalcifyPower>(Owner.Creature, DynamicVars["CalcifyPower"].BaseValue, Owner.Creature, null);
	}
}
