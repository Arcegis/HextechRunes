namespace HextechRunes;

public sealed class DonationRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new GoldVar(1000)
	];

	public override Task AfterObtained()
	{
		return PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner!);
	}
}
