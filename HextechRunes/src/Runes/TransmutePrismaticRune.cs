using MegaCrit.Sts2.Core.Entities.Players;

namespace HextechRunes;

public sealed class TransmutePrismaticRune : HextechRelicBase
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
		await HextechRuneGrantHelper.ConsumeAndObtainRandomRunes(this, player, HextechCatalog.GetConfigurablePlayerRuneTypesForRarity(HextechRarityTier.Prismatic), 1);
	}
}
