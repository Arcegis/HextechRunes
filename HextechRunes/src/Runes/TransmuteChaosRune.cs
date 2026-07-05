namespace HextechRunes;

public sealed class TransmuteChaosRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Player player = Owner;
		Flash();
		await HextechRuneGrantHelper.ConsumeAndObtainRandomRunes(this, player, HextechCatalog.GetAllConfigurableRuneTypes(), 2);
	}
}
