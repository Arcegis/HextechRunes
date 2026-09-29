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
		return Owner == null
			? Task.CompletedTask
			: PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
	}
}
