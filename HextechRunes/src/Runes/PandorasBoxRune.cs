namespace HextechRunes;

public sealed class PandorasBoxRune : HextechRelicBase
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
		await HextechRuneGrantHelper.ReplaceOwnedHextechRunesWithRandomRunes(
			player,
			HextechCatalog.GetConfigurablePlayerRuneTypesForRarity(HextechRarityTier.Prismatic),
			new HashSet<ModelId> { ModelDb.GetId<PandorasBoxRune>() });
	}
}
