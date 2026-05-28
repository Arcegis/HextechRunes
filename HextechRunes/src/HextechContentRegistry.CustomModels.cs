namespace HextechRunes;

internal static partial class HextechContentRegistry
{
    internal static readonly IReadOnlyList<Type> ShopOnlyRelicTypes =
    [
        typeof(RandomForgeShopRelic)
    ];

    internal static readonly IReadOnlyList<Type> CustomCardTypes =
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
