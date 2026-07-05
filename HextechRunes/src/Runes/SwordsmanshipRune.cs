namespace HextechRunes;

public sealed class SwordsmanshipRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<ParryPower>(12m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ParryPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override async Task BeforeCombatStart()
	{
		if (Owner == null || Owner.Creature.IsDead || !IsRegentOwner)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<ParryPower>(Owner.Creature, DynamicVars["ParryPower"].BaseValue, Owner.Creature, null);
	}
}
