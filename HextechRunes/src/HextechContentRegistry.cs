namespace HextechRunes;

internal static partial class HextechContentRegistry
{
    [Flags]
    private enum RuneFlags
    {
        None = 0,
        Disabled = 1,
        AttributeConversionExclusive = 2,
        FirstActExcluded = 4,
        ThirdActExcluded = 8
    }

    private enum HextechCharacterPool
    {
        Ironclad,
        Silent,
        Regent,
        Defect,
        Necrobinder
    }

    private readonly record struct RuneRegistration(
        Type Type,
        HextechRarityTier Rarity,
        RuneFlags Flags = RuneFlags.None,
        HextechCharacterPool? CharacterPool = null,
        int CharacterOrder = 0);

    private readonly record struct ForgeRegistration(Type Type, HextechRarityTier Rarity);

	private readonly record struct MonsterHexRegistration(
		MonsterHexKind Kind,
		HextechRarityTier Rarity,
		Type IconRelicType,
		bool Disabled = false,
		bool HasBurnHoverTip = false);

    internal static IReadOnlyList<Type> SilverRuneTypes => RuneTypesForRarity(HextechRarityTier.Silver);

    internal static IReadOnlyList<Type> GoldRuneTypes => RuneTypesForRarity(HextechRarityTier.Gold);

    internal static IReadOnlyList<Type> PrismaticRuneTypes => RuneTypesForRarity(HextechRarityTier.Prismatic);

    internal static IReadOnlyList<Type> SilverForgeTypes => ForgeTypesForRarity(HextechRarityTier.Silver);

    internal static IReadOnlyList<Type> GoldForgeTypes => ForgeTypesForRarity(HextechRarityTier.Gold);

    internal static IReadOnlyList<Type> PrismaticForgeTypes => ForgeTypesForRarity(HextechRarityTier.Prismatic);

    internal static IReadOnlySet<Type> DisabledPlayerRuneTypes => RuneTypesWithFlag(RuneFlags.Disabled).ToHashSet();

    internal static IReadOnlyList<Type> IroncladRuneTypes => RuneTypesForCharacter(HextechCharacterPool.Ironclad);

    internal static IReadOnlyList<Type> SilentRuneTypes => RuneTypesForCharacter(HextechCharacterPool.Silent);

    internal static IReadOnlyList<Type> RegentRuneTypes => RuneTypesForCharacter(HextechCharacterPool.Regent);

    internal static IReadOnlyList<Type> DefectRuneTypes => RuneTypesForCharacter(HextechCharacterPool.Defect);

    internal static IReadOnlyList<Type> NecrobinderRuneTypes => RuneTypesForCharacter(HextechCharacterPool.Necrobinder);

    internal static IReadOnlyList<Type> AttributeConversionExclusiveRuneTypes => RuneTypesWithFlag(RuneFlags.AttributeConversionExclusive);

    internal static IReadOnlySet<Type> FirstActExcludedRuneTypes => RuneTypesWithFlag(RuneFlags.FirstActExcluded).ToHashSet();

    internal static IReadOnlySet<Type> ThirdActExcludedRuneTypes => RuneTypesWithFlag(RuneFlags.ThirdActExcluded).ToHashSet();

    internal static IReadOnlySet<MonsterHexKind> DisabledMonsterHexes => MonsterHexRegistrations
        .Where(static registration => registration.Disabled)
        .Select(static registration => registration.Kind)
        .ToHashSet();

	internal static IReadOnlySet<MonsterHexKind> MonsterHexesWithBurnHoverTip => MonsterHexRegistrations
		.Where(static registration => registration.HasBurnHoverTip)
		.Select(static registration => registration.Kind)
		.ToHashSet();

	internal static IReadOnlyDictionary<MonsterHexKind, Type> MonsterHexIconRelicTypes => MonsterHexRegistrations
		.ToDictionary(static registration => registration.Kind, static registration => registration.IconRelicType);

    internal static IReadOnlyList<MonsterHexKind> SilverMonsterHexes => MonsterHexesForRarity(HextechRarityTier.Silver);

    internal static IReadOnlyList<MonsterHexKind> GoldMonsterHexes => MonsterHexesForRarity(HextechRarityTier.Gold);

    internal static IReadOnlyList<MonsterHexKind> PrismaticMonsterHexes => MonsterHexesForRarity(HextechRarityTier.Prismatic);

    internal static IReadOnlyList<Type> AllRuneTypes => SilverRuneTypes
        .Concat(GoldRuneTypes)
        .Concat(PrismaticRuneTypes)
        .Distinct()
        .ToArray();

    internal static IReadOnlyList<Type> AllForgeTypes => SilverForgeTypes
        .Concat(GoldForgeTypes)
        .Concat(PrismaticForgeTypes)
        .Distinct()
        .ToArray();

    internal static IReadOnlyList<Type> AllCustomRelicTypes => AllRuneTypes
        .Concat(AllForgeTypes)
        .Concat(ShopOnlyRelicTypes)
        .Distinct()
        .ToArray();

    private static RuneRegistration Rune<TRune>(
        HextechRarityTier rarity,
        RuneFlags flags = RuneFlags.None,
        HextechCharacterPool? characterPool = null,
        int characterOrder = 0)
    {
        return new RuneRegistration(typeof(TRune), rarity, flags, characterPool, characterOrder);
    }

    private static ForgeRegistration Forge<TForge>(HextechRarityTier rarity)
    {
        return new ForgeRegistration(typeof(TForge), rarity);
    }

	private static MonsterHexRegistration Monster<TRelic>(
		MonsterHexKind kind,
		HextechRarityTier rarity,
		bool disabled = false,
		bool hasBurnHoverTip = false)
	{
		return new MonsterHexRegistration(kind, rarity, typeof(TRelic), disabled, hasBurnHoverTip);
	}

    private static IReadOnlyList<Type> RuneTypesForRarity(HextechRarityTier rarity)
    {
        return RuneRegistrations
            .Where(registration => registration.Rarity == rarity)
            .Select(static registration => registration.Type)
            .ToArray();
    }

    private static IReadOnlyList<Type> ForgeTypesForRarity(HextechRarityTier rarity)
    {
        return ForgeRegistrations
            .Where(registration => registration.Rarity == rarity)
            .Select(static registration => registration.Type)
            .ToArray();
    }

    private static IReadOnlyList<Type> RuneTypesForCharacter(HextechCharacterPool characterPool)
    {
        return RuneRegistrations
            .Where(registration => registration.CharacterPool == characterPool)
            .OrderBy(static registration => registration.CharacterOrder)
            .Select(static registration => registration.Type)
            .ToArray();
    }

    private static IReadOnlyList<Type> RuneTypesWithFlag(RuneFlags flag)
    {
        return RuneRegistrations
            .Where(registration => (registration.Flags & flag) != 0)
            .Select(static registration => registration.Type)
            .ToArray();
    }

    private static IReadOnlyList<MonsterHexKind> MonsterHexesForRarity(HextechRarityTier rarity)
    {
        return MonsterHexRegistrations
            .Where(registration => registration.Rarity == rarity && !registration.Disabled)
            .Select(static registration => registration.Kind)
            .ToArray();
    }
}
