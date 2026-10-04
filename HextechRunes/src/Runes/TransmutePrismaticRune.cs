namespace HextechRunes;

public sealed class TransmutePrismaticRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override async Task AfterObtained()
	{
		Flash();
		await HextechRuneGrantHelper.ConsumeAndObtainRandomRunes(this, Owner, HextechCatalog.GetConfigurablePlayerRuneTypesForRarity(HextechRarityTier.Prismatic), 1);
	}
}
