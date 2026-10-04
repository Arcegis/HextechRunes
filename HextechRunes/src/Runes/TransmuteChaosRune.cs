namespace HextechRunes;

public sealed class TransmuteChaosRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override async Task AfterObtained()
	{
		Flash();
		await HextechRuneGrantHelper.ConsumeAndObtainRandomRunes(this, Owner, HextechCatalog.GetAllConfigurableRuneTypes(), 2);
	}
}
