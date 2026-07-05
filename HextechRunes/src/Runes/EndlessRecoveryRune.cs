namespace HextechRunes;

public sealed class EndlessRecoveryRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("HealPercent", 10m)
	];

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		int heal = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * DynamicVars["HealPercent"].BaseValue / 100m));
		return CreatureCmd.Heal(Owner.Creature, heal);
	}
}
