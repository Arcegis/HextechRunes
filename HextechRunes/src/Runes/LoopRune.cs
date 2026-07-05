namespace HextechRunes;

public sealed class LoopRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1)
	];

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		return player == Owner ? amount + DynamicVars.Energy.BaseValue : amount;
	}
}
