namespace HextechRunes;

public sealed class StatsOnStatsOnStatsRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("ForgeCount", 6m)
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
