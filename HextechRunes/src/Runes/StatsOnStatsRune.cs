namespace HextechRunes;

public sealed class StatsOnStatsRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ForgeCount", 4m)
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Flash();
		await HextechForgeGrantHelper.ObtainRandomForges(Owner, DynamicVars["ForgeCount"].IntValue);
	}
}
