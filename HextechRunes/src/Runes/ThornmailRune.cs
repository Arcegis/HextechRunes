namespace HextechRunes;

public sealed class ThornmailRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ThornsPower>()
	];

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is not CombatRoom || Owner == null)
		{
			return Task.CompletedTask;
		}

		Flash();
		int thorns = 2 + Math.Min(3, FloorToInt(Owner.Creature.MaxHp / 40m));
		return PowerCmd.Apply<ThornsPower>(Owner.Creature, thorns, Owner.Creature, null);
	}
}
