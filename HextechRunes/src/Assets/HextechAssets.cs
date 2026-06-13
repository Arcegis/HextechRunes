using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static class HextechAssets
{
    public const string HextechSubcategoryKey = "HEXTECH_RUNES_SUBCATEGORY";

    public const string ForgeSubcategoryKey = "HEXTECH_FORGES_SUBCATEGORY";

    public const string ElicitCardPortraitPath = "res://HextechRunes/images/cards/elicitCard.png";

    public const string TrickMagicCardPortraitPath = "res://HextechRunes/images/cards/trickMagicCard.png";

    public const string BladeWaltzCardPortraitPath = "res://HextechRunes/images/cards/bladeWaltzCard.png";

    public const string CatalystCardPortraitPath = "res://HextechRunes/images/cards/catalystCard.png";

    public const string AllInCardPortraitPath = "res://HextechRunes/images/cards/allInCard.png";

    public const string WhiteHoleCardPortraitPath = "res://HextechRunes/images/cards/whiteHoleCard.png";

    public const string SearingAttackCardPortraitPath = "res://HextechRunes/images/cards/searingAttackCard.png";

    public const string OstyWishCardPortraitPath = "res://HextechRunes/images/cards/ostyWishCard.png";

    public const string OceanDragonSoulCardPortraitPath = "res://HextechRunes/images/cards/oceanDragonSoulCard.png";

    public const string InfernalDragonSoulCardPortraitPath = "res://HextechRunes/images/cards/infernalDragonSoulCard.png";

    public const string HextechDragonSoulCardPortraitPath = "res://HextechRunes/images/cards/hextechDragonSoulCard.png";

    public const string MountainDragonSoulCardPortraitPath = "res://HextechRunes/images/cards/mountainDragonSoulCard.png";

    public const string ChemtechDragonSoulCardPortraitPath = "res://HextechRunes/images/cards/chemtechDragonSoulCard.png";

    public const string CloudDragonSoulCardPortraitPath = "res://HextechRunes/images/cards/cloudDragonSoulCard.png";

    public const string OceanDragonSoulPowerIconPath = "res://HextechRunes/images/powers/oceanDragonSoulPower.png";

    public const string InfernalDragonSoulPowerIconPath = "res://HextechRunes/images/powers/infernalDragonSoulPower.png";

    public const string HextechDragonSoulPowerIconPath = "res://HextechRunes/images/powers/hextechDragonSoulPower.png";

    public const string MountainDragonSoulPowerIconPath = "res://HextechRunes/images/powers/mountainDragonSoulPower.png";

    public const string ChemtechDragonSoulPowerIconPath = "res://HextechRunes/images/powers/chemtechDragonSoulPower.png";

    public const string CloudDragonSoulPowerIconPath = "res://HextechRunes/images/powers/cloudDragonSoulPower.png";

    public static string? TryGetCustomRelicIconPath(RelicModel relic)
    {
        if (HextechCatalog.IsHextechRelic(relic))
        {
            ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
            return $"res://{ModInfo.Id}/images/relics/{ToImageFileStem(id.Entry)}.png";
        }

        if (HextechCatalog.TryGetForgeRarity(relic, out HextechRarityTier forgeRarity))
        {
            return GetForgeIconPath(forgeRarity);
        }

        if (HextechCatalog.IsHextechShopRelic(relic))
        {
            return $"res://{ModInfo.Id}/images/relics/silverForge.png";
        }

        return null;
    }

    public static string GetForgeIconPath(HextechRarityTier rarity)
    {
        string iconStem = rarity switch
        {
            HextechRarityTier.Silver => "silverForge",
            HextechRarityTier.Gold => "goldForge",
            HextechRarityTier.Prismatic => "prismaticForge",
            _ => "silverForge"
        };
        return $"res://{ModInfo.Id}/images/relics/{iconStem}.png";
    }

    internal static string ToImageFileStem(string entry)
    {
        string[] parts = entry.ToLowerInvariant().Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return entry;
        }

        return parts[0] + string.Concat(parts.Skip(1).Select(static part => char.ToUpperInvariant(part[0]) + part[1..]));
    }
}
