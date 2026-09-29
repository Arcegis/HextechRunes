namespace HextechRunes;

public sealed class WizardlyThinkingRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<FocusPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterRoomEntered(AbstractRoom room)
	{
		// 发放闸门之外，外部接口(RelicBundleGrantHelper)、控制台或其他模组可把本符文直接给任意角色，触发时再判角色。
		if (room is not CombatRoom || Owner == null || !IsDefectOwner)
		{
			return;
		}

		int focus = GetPlayerActNumberForScaling();
		Flash();
		await PowerCmd.Apply<FocusPower>(Owner.Creature, focus, Owner.Creature, null);
	}
}
