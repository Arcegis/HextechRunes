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
		// 发放闸门之外，外部接口(RelicBundleGrantHelper)、控制台或其他模组可把本符文直接给任意角色，触发时再判角色。
		if (Owner.Creature.IsDead || !IsRegentOwner)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<ParryPower>(Owner.Creature, DynamicVars["ParryPower"].BaseValue, Owner.Creature, null);
	}
}
