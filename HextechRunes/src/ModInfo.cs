using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static class ModInfo
{
    public readonly record struct RuneSeriesGroup(string LocalizationKey, IReadOnlyList<RelicModel> Relics);

    public readonly record struct CharacterRunePool(string LocalizationKey, IReadOnlyList<Type> RuneTypes);

    public const string Id = "HextechRunes";

    public const string DisplayName = "海克斯符文";

    public const string Version = "0.5.0";

    public const string TargetGameVersion = "0.103.2";

    public const string HextechSubcategoryKey = "HEXTECH_RUNES_SUBCATEGORY";

    public const string ForgeSubcategoryKey = "HEXTECH_FORGES_SUBCATEGORY";

    private static readonly IReadOnlyList<Type> SilverRuneTypes = HextechContentRegistry.SilverRuneTypes;

    private static readonly IReadOnlyList<Type> GoldRuneTypes = HextechContentRegistry.GoldRuneTypes;

    private static readonly IReadOnlyList<Type> PrismaticRuneTypes = HextechContentRegistry.PrismaticRuneTypes;

    private static readonly IReadOnlyList<Type> SilverForgeTypes = HextechContentRegistry.SilverForgeTypes;

    private static readonly IReadOnlyList<Type> GoldForgeTypes = HextechContentRegistry.GoldForgeTypes;

    private static readonly IReadOnlyList<Type> PrismaticForgeTypes = HextechContentRegistry.PrismaticForgeTypes;

    private static readonly IReadOnlyList<Type> ShopOnlyRelicTypes = HextechContentRegistry.ShopOnlyRelicTypes;

    private static readonly IReadOnlySet<Type> DisabledPlayerRuneTypes = HextechContentRegistry.DisabledPlayerRuneTypes;

    private static readonly IReadOnlyList<CharacterRunePool> CharacterRunePools =
    [
        new("IRONCLAD", HextechContentRegistry.IroncladRuneTypes),
        new("SILENT", HextechContentRegistry.SilentRuneTypes),
        new("REGENT", HextechContentRegistry.RegentRuneTypes),
        new("DEFECT", HextechContentRegistry.DefectRuneTypes),
        new("NECROBINDER", HextechContentRegistry.NecrobinderRuneTypes)
    ];

    private static readonly IReadOnlySet<Type> CharacterSpecificRuneTypes = CharacterRunePools
        .SelectMany(static pool => pool.RuneTypes)
        .ToHashSet();

    private static readonly IReadOnlyList<Type> AttributeConversionExclusiveRuneTypes = HextechContentRegistry.AttributeConversionExclusiveRuneTypes;

    private static readonly IReadOnlySet<Type> FirstActExcludedRuneTypes = HextechContentRegistry.FirstActExcludedRuneTypes;

    private static readonly IReadOnlySet<Type> ThirdActExcludedRuneTypes = HextechContentRegistry.ThirdActExcludedRuneTypes;

    private static readonly IReadOnlyList<MonsterHexKind> SilverMonsterHexes = HextechContentRegistry.SilverMonsterHexes;

    private static readonly IReadOnlyList<MonsterHexKind> GoldMonsterHexes = HextechContentRegistry.GoldMonsterHexes;

    private static readonly IReadOnlyList<MonsterHexKind> PrismaticMonsterHexes = HextechContentRegistry.PrismaticMonsterHexes;

    private static readonly IReadOnlyDictionary<MonsterHexKind, Type> MonsterHexIconRelicTypes = HextechContentRegistry.MonsterHexIconRelicTypes;

    private static readonly IReadOnlyList<Type> AllRuneTypes = HextechContentRegistry.AllRuneTypes;

    private static readonly IReadOnlyList<Type> AllForgeTypes = HextechContentRegistry.AllForgeTypes;

    private static readonly IReadOnlyList<Type> AllCustomRelicTypes = HextechContentRegistry.AllCustomRelicTypes;

    private static readonly IReadOnlyList<Type> CustomCardTypes = HextechContentRegistry.CustomCardTypes;

    public static IReadOnlyList<Type> GetAllRuneTypes() => AllRuneTypes;

    public static IReadOnlyList<Type> GetAllSelectableRuneTypes()
    {
        return Enum.GetValues<HextechRarityTier>()
            .SelectMany(GetPlayerRuneTypesForRarity)
            .ToArray();
    }

    public static IReadOnlyList<Type> GetGenericSelectableRuneTypes()
    {
        return GetAllSelectableRuneTypes()
            .Where(static type => !CharacterSpecificRuneTypes.Contains(type))
            .ToArray();
    }

    public static IReadOnlyList<Type> GetAllForgeTypes() => AllForgeTypes;

    public static IReadOnlyList<Type> GetAllCustomRelicTypes() => AllCustomRelicTypes;

    public static IReadOnlyList<Type> GetAllCustomCardTypes() => CustomCardTypes;

    public static IReadOnlyList<Type> GetPlayerRuneTypesForRarity(HextechRarityTier rarity)
    {
        IReadOnlyList<Type> runeTypes = rarity switch
        {
            HextechRarityTier.Silver => SilverRuneTypes,
            HextechRarityTier.Gold => GoldRuneTypes,
            HextechRarityTier.Prismatic => PrismaticRuneTypes,
            _ => Array.Empty<Type>()
        };
        return runeTypes.Where(static type => !DisabledPlayerRuneTypes.Contains(type)).ToArray();
    }

    public static IReadOnlyList<Type> GetForgeTypesForRarity(HextechRarityTier rarity)
    {
        return rarity switch
        {
            HextechRarityTier.Silver => SilverForgeTypes,
            HextechRarityTier.Gold => GoldForgeTypes,
            HextechRarityTier.Prismatic => PrismaticForgeTypes,
            _ => Array.Empty<Type>()
        };
    }

    public static IReadOnlyList<MonsterHexKind> GetMonsterHexesForRarity(HextechRarityTier rarity)
    {
        return rarity switch
        {
            HextechRarityTier.Silver => SilverMonsterHexes,
            HextechRarityTier.Gold => GoldMonsterHexes,
            HextechRarityTier.Prismatic => PrismaticMonsterHexes,
            _ => Array.Empty<MonsterHexKind>()
        };
    }

    public static HextechRarityTier GetMonsterHexRarity(MonsterHexKind hex)
    {
        if (SilverMonsterHexes.Contains(hex))
        {
            return HextechRarityTier.Silver;
        }

        if (GoldMonsterHexes.Contains(hex))
        {
            return HextechRarityTier.Gold;
        }

        if (PrismaticMonsterHexes.Contains(hex))
        {
            return HextechRarityTier.Prismatic;
        }

        throw new ArgumentOutOfRangeException(nameof(hex), hex, "Unknown monster hex rarity.");
    }

    public static RelicModel GetIconRelicForMonsterHex(MonsterHexKind hex)
    {
        if (!MonsterHexIconRelicTypes.TryGetValue(hex, out Type? relicType))
        {
            throw new ArgumentOutOfRangeException(nameof(hex), hex, "Unknown monster hex icon relic.");
        }

        return ModelDb.GetById<RelicModel>(ModelDb.GetId(relicType));
    }

    public static bool TryGetMonsterHexKind(RelicModel relic, out MonsterHexKind hex)
    {
        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        foreach (KeyValuePair<MonsterHexKind, Type> pair in MonsterHexIconRelicTypes)
        {
            RelicModel iconRelic = ModelDb.GetById<RelicModel>(ModelDb.GetId(pair.Value));
            if ((iconRelic.CanonicalInstance?.Id ?? iconRelic.Id) == id)
            {
                hex = pair.Key;
                return true;
            }
        }

        hex = default;
        return false;
    }

    public static string GetEnemyHexDescriptionFormatted(MonsterHexKind hex)
    {
        return GetEnemyHexDescriptionLoc(hex).GetFormattedText();
    }

    public static IEnumerable<IHoverTip> GetEnemyHexHoverTips(MonsterHexKind hex)
    {
        RelicModel relic = GetIconRelicForMonsterHex(hex);
        HoverTip mainTip = new(relic.Title, GetEnemyHexDescriptionFormatted(hex), relic.Icon);
        return [mainTip];
    }

    private static LocString GetEnemyHexDescriptionLoc(MonsterHexKind hex)
    {
        RelicModel relic = GetIconRelicForMonsterHex(hex);
        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        return new LocString("relics", ToImageFileStem(id.Entry) + ".enemyDescription");
    }

    public static IReadOnlyList<RelicModel> GetCanonicalRunes()
    {
        return AllRuneTypes
            .Select(static type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
            .ToArray();
    }

    public static IReadOnlyList<RelicModel> GetCanonicalSelectableRunes()
    {
        return GetAllSelectableRuneTypes()
            .Select(static type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
            .ToArray();
    }

    public static IReadOnlyList<RelicModel> GetCanonicalGenericSelectableRunes()
    {
        return GetGenericSelectableRuneTypes()
            .Select(static type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
            .ToArray();
    }

    public static IReadOnlyList<RuneSeriesGroup> GetCharacterRuneGroups()
    {
        return CharacterRunePools
            .Select(static pool => new RuneSeriesGroup(
                $"CHARACTER.{pool.LocalizationKey}",
                pool.RuneTypes
                    .Select(static (type, index) => new IndexedRuneType(type, index))
                    .Where(static rune => !DisabledPlayerRuneTypes.Contains(rune.Type))
                    .OrderBy(static rune => GetPlayerRuneRaritySortOrder(rune.Type))
                    .ThenBy(static rune => rune.Index)
                    .Select(static rune => ModelDb.GetById<RelicModel>(ModelDb.GetId(rune.Type)))
                    .ToArray()))
            .Where(static group => group.Relics.Count > 0)
            .ToArray();
    }

    private readonly record struct IndexedRuneType(Type Type, int Index);

    private static int GetPlayerRuneRaritySortOrder(Type type)
    {
        if (SilverRuneTypes.Contains(type))
        {
            return 0;
        }

        if (GoldRuneTypes.Contains(type))
        {
            return 1;
        }

        if (PrismaticRuneTypes.Contains(type))
        {
            return 2;
        }

        return 3;
    }

    public static IReadOnlyList<RelicModel> GetCanonicalForges()
    {
        return AllForgeTypes
            .Select(static type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
            .ToArray();
    }

    public static IReadOnlyList<RelicModel> GetCanonicalCustomRelics()
    {
        return AllCustomRelicTypes
            .Select(static type => ModelDb.GetById<RelicModel>(ModelDb.GetId(type)))
            .ToArray();
    }

    public static bool IsHextechRelic(RelicModel? relic)
    {
        if (relic == null)
        {
            return false;
        }

        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        return AllRuneTypes.Any(type => id == ModelDb.GetId(type));
    }

    public static bool IsHextechForgeRelic(RelicModel? relic)
    {
        if (relic == null)
        {
            return false;
        }

        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        return AllForgeTypes.Any(type => id == ModelDb.GetId(type));
    }

    public static bool IsHextechShopRelic(RelicModel? relic)
    {
        if (relic == null)
        {
            return false;
        }

        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        return ShopOnlyRelicTypes.Any(type => id == ModelDb.GetId(type));
    }

    public static bool IsHextechCustomRelic(RelicModel? relic)
    {
        return IsHextechRelic(relic) || IsHextechForgeRelic(relic) || IsHextechShopRelic(relic);
    }

    public static bool TryGetPlayerRuneRarity(RelicModel? relic, out HextechRarityTier rarity)
    {
        rarity = default;
        if (relic == null)
        {
            return false;
        }

        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        if (SilverRuneTypes.Any(type => id == ModelDb.GetId(type)))
        {
            rarity = HextechRarityTier.Silver;
            return true;
        }

        if (GoldRuneTypes.Any(type => id == ModelDb.GetId(type)))
        {
            rarity = HextechRarityTier.Gold;
            return true;
        }

        if (PrismaticRuneTypes.Any(type => id == ModelDb.GetId(type)))
        {
            rarity = HextechRarityTier.Prismatic;
            return true;
        }

        return false;
    }

    public static bool TryGetForgeRarity(RelicModel? relic, out HextechRarityTier rarity)
    {
        rarity = default;
        if (relic == null)
        {
            return false;
        }

        ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
        if (SilverForgeTypes.Any(type => id == ModelDb.GetId(type)))
        {
            rarity = HextechRarityTier.Silver;
            return true;
        }

        if (GoldForgeTypes.Any(type => id == ModelDb.GetId(type)))
        {
            rarity = HextechRarityTier.Gold;
            return true;
        }

        if (PrismaticForgeTypes.Any(type => id == ModelDb.GetId(type)))
        {
            rarity = HextechRarityTier.Prismatic;
            return true;
        }

        return false;
    }

    public static bool IsAvailableForPlayer(RelicModel relic, Player player)
    {
        return relic is not HextechRelicBase hextechRelic || hextechRelic.IsAvailableForPlayer(player);
    }

    public static bool IsPlayerRuneAllowedInAct(Type runeType, int actIndex)
    {
        return actIndex switch
        {
            0 => !FirstActExcludedRuneTypes.Contains(runeType),
            2 => !ThirdActExcludedRuneTypes.Contains(runeType),
            _ => true
        };
    }

    public static IReadOnlySet<ModelId> GetMutuallyExclusivePlayerRuneIds(IEnumerable<ModelId> ownedIds)
    {
        HashSet<ModelId> ownedSet = ownedIds.ToHashSet();
        if (!ownedSet.Any(IsAttributeConversionExclusiveRuneId))
        {
            return new HashSet<ModelId>();
        }

        HashSet<ModelId> blockedIds = new();
        foreach (Type runeType in AttributeConversionExclusiveRuneTypes)
        {
            ModelId candidateId = ModelDb.GetId(runeType);
            if (!ownedSet.Contains(candidateId))
            {
                blockedIds.Add(candidateId);
            }
        }

        return blockedIds;
    }

    public static string? TryGetCustomRelicIconPath(RelicModel relic)
    {
        if (IsHextechRelic(relic))
        {
            ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
            return $"res://{Id}/images/relics/{ToImageFileStem(id.Entry)}.png";
        }

        if (TryGetForgeRarity(relic, out HextechRarityTier forgeRarity))
        {
            string iconStem = forgeRarity switch
            {
                HextechRarityTier.Silver => "silverForge",
                HextechRarityTier.Gold => "goldForge",
                HextechRarityTier.Prismatic => "prismaticForge",
                _ => "silverForge"
            };
            return $"res://{Id}/images/relics/{iconStem}.png";
        }

        if (IsHextechShopRelic(relic))
        {
            return $"res://{Id}/images/relics/silverForge.png";
        }

        return null;
    }

    private static string ToImageFileStem(string entry)
    {
        string[] parts = entry.ToLowerInvariant().Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return entry;
        }

        return parts[0] + string.Concat(parts.Skip(1).Select(static part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static bool IsAttributeConversionExclusiveRuneId(ModelId id)
    {
        return AttributeConversionExclusiveRuneTypes.Any(type => ModelDb.GetId(type) == id);
    }

    public static IReadOnlyList<RuneSeriesGroup> GetRuneSeriesGroups(IReadOnlyList<RelicModel> relics)
    {
        Dictionary<ModelId, RelicModel> byId = relics.ToDictionary(static relic => relic.CanonicalInstance?.Id ?? relic.Id);

        IReadOnlyList<RelicModel> BuildGroup(IEnumerable<Type> runeTypes)
        {
            List<RelicModel> group = new();
            foreach (Type runeType in runeTypes)
            {
                if (DisabledPlayerRuneTypes.Contains(runeType))
                {
                    continue;
                }

                ModelId id = ModelDb.GetId(runeType);
                if (byId.TryGetValue(id, out RelicModel? relic))
                {
                    group.Add(relic);
                }
            }

            return group;
        }

        return
        [
            new RuneSeriesGroup("SILVER", BuildGroup(SilverRuneTypes)),
            new RuneSeriesGroup("GOLD", BuildGroup(GoldRuneTypes)),
            new RuneSeriesGroup("PRISMATIC", BuildGroup(PrismaticRuneTypes))
        ];
    }

    public static IReadOnlyList<RuneSeriesGroup> GetForgeSeriesGroups()
    {
        IReadOnlyList<RelicModel> relics = GetCanonicalForges();
        Dictionary<ModelId, RelicModel> byId = relics.ToDictionary(static relic => relic.CanonicalInstance?.Id ?? relic.Id);

        IReadOnlyList<RelicModel> BuildGroup(IEnumerable<Type> forgeTypes)
        {
            List<RelicModel> group = new();
            foreach (Type forgeType in forgeTypes)
            {
                ModelId id = ModelDb.GetId(forgeType);
                if (byId.TryGetValue(id, out RelicModel? relic))
                {
                    group.Add(relic);
                }
            }

            return group;
        }

        return
        [
            new RuneSeriesGroup("SILVER", BuildGroup(SilverForgeTypes)),
            new RuneSeriesGroup("GOLD", BuildGroup(GoldForgeTypes)),
            new RuneSeriesGroup("PRISMATIC", BuildGroup(PrismaticForgeTypes))
        ];
    }
}
