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
		if (room is not CombatRoom || Owner == null || !IsDefectOwner)
		{
			return;
		}

		int focus = GetPlayerActNumberForScaling();
		Flash();
		await PowerCmd.Apply<FocusPower>(Owner.Creature, focus, Owner.Creature, null);
	}
}
