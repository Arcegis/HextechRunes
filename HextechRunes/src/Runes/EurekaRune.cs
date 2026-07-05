namespace HextechRunes;

public sealed class EurekaRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("RelicsNeeded", 6m),
		new EnergyVar(1)
	];

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		if (player != Owner)
		{
			return amount;
		}

		return amount + FloorToInt(player.Relics.Count / DynamicVars["RelicsNeeded"].BaseValue);
	}
}
