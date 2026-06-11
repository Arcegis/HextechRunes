namespace HextechRunes;

internal static class HextechCustomModelRegistry
{
	internal static IReadOnlyList<Type> EventRelicTypes { get; } =
	[
		typeof(GoldStarRelic)
	];

	internal static IReadOnlyList<Type> ShopOnlyRelicTypes { get; } =
	[
		typeof(RandomForgeShopRelic)
	];

	internal static IReadOnlyList<Type> CustomCardTypes { get; } =
	[
		typeof(ElicitCard),
		typeof(TrickMagicCard),
		typeof(BladeWaltzCard),
		typeof(CatalystCard),
		typeof(AllInCard),
		typeof(WhiteHoleCard),
		typeof(SearingAttackCard),
		typeof(OstyWishCard),
		typeof(OceanDragonSoulCard),
		typeof(InfernalDragonSoulCard),
		typeof(HextechDragonSoulCard),
		typeof(MountainDragonSoulCard),
		typeof(ChemtechDragonSoulCard),
		typeof(CloudDragonSoulCard)
	];
}
