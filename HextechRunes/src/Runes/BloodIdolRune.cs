namespace HextechRunes;

public sealed class BloodIdolRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new HealVar(5m)
	];

	public override Task AfterGoldGained(Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
	}
}
