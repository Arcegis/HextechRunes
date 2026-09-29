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

		Flash();
		await HextechRuneGrantHelper.ReplaceOwnedHextechRunesWithRandomRunes(
			Owner,
			HextechCatalog.GetConfigurablePlayerRuneTypesForRarity(HextechRarityTier.Prismatic),
			$"pandoras-box:{Id.Category}:{Id.Entry}",
			new HashSet<ModelId> { ModelDb.GetId<PandorasBoxRune>() });
	}
}
