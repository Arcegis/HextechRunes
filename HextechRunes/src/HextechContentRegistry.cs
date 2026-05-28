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

	private static readonly Lazy<RegistryLookups> LazyLookups = new(BuildRegistryLookups);

	private static RegistryLookups Lookups => LazyLookups.Value;

    internal static IReadOnlyList<Type> SilverRuneTypes => Lookups.SilverRuneTypes;

    internal static IReadOnlyList<Type> GoldRuneTypes => Lookups.GoldRuneTypes;

    internal static IReadOnlyList<Type> PrismaticRuneTypes => Lookups.PrismaticRuneTypes;

    internal static IReadOnlyList<Type> SilverForgeTypes => Lookups.SilverForgeTypes;

    internal static IReadOnlyList<Type> GoldForgeTypes => Lookups.GoldForgeTypes;

    internal static IReadOnlyList<Type> PrismaticForgeTypes => Lookups.PrismaticForgeTypes;

    internal static IReadOnlySet<Type> DisabledPlayerRuneTypes => Lookups.DisabledPlayerRuneTypes;

    internal static IReadOnlyList<Type> IroncladRuneTypes => Lookups.IroncladRuneTypes;

    internal static IReadOnlyList<Type> SilentRuneTypes => Lookups.SilentRuneTypes;

    internal static IReadOnlyList<Type> RegentRuneTypes => Lookups.RegentRuneTypes;

    internal static IReadOnlyList<Type> DefectRuneTypes => Lookups.DefectRuneTypes;

    internal static IReadOnlyList<Type> NecrobinderRuneTypes => Lookups.NecrobinderRuneTypes;

    internal static IReadOnlyList<Type> AttributeConversionExclusiveRuneTypes => Lookups.AttributeConversionExclusiveRuneTypes;

    internal static IReadOnlySet<Type> FirstActExcludedRuneTypes => Lookups.FirstActExcludedRuneTypes;

    internal static IReadOnlySet<Type> ThirdActExcludedRuneTypes => Lookups.ThirdActExcludedRuneTypes;

    internal static IReadOnlySet<MonsterHexKind> DisabledMonsterHexes => Lookups.DisabledMonsterHexes;

	internal static IReadOnlySet<MonsterHexKind> MonsterHexesWithBurnHoverTip => Lookups.MonsterHexesWithBurnHoverTip;

	internal static IReadOnlyDictionary<MonsterHexKind, Type> MonsterHexIconRelicTypes => Lookups.MonsterHexIconRelicTypes;

	internal static IReadOnlySet<MonsterHexKind> AllMonsterHexKinds => Lookups.AllMonsterHexKinds;

    internal static IReadOnlyList<MonsterHexKind> SilverMonsterHexes => Lookups.SilverMonsterHexes;

    internal static IReadOnlyList<MonsterHexKind> GoldMonsterHexes => Lookups.GoldMonsterHexes;

    internal static IReadOnlyList<MonsterHexKind> PrismaticMonsterHexes => Lookups.PrismaticMonsterHexes;

    internal static IReadOnlyList<Type> AllRuneTypes => Lookups.AllRuneTypes;

    internal static IReadOnlyList<Type> AllForgeTypes => Lookups.AllForgeTypes;

    internal static IReadOnlyList<Type> AllCustomRelicTypes => Lookups.AllCustomRelicTypes;

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

	private static RegistryLookups BuildRegistryLookups()
	{
		return new RegistryLookups(RuneRegistrations, ForgeRegistrations, MonsterHexRegistrations, ShopOnlyRelicTypes);
	}

	private sealed class RegistryLookups
	{
		public RegistryLookups(
			IReadOnlyList<RuneRegistration> runeRegistrations,
			IReadOnlyList<ForgeRegistration> forgeRegistrations,
			IReadOnlyList<MonsterHexRegistration> monsterHexRegistrations,
			IReadOnlyList<Type> shopOnlyRelicTypes)
		{
			SilverRuneTypes = RuneTypesForRarity(runeRegistrations, HextechRarityTier.Silver);
			GoldRuneTypes = RuneTypesForRarity(runeRegistrations, HextechRarityTier.Gold);
			PrismaticRuneTypes = RuneTypesForRarity(runeRegistrations, HextechRarityTier.Prismatic);
			SilverForgeTypes = ForgeTypesForRarity(forgeRegistrations, HextechRarityTier.Silver);
			GoldForgeTypes = ForgeTypesForRarity(forgeRegistrations, HextechRarityTier.Gold);
			PrismaticForgeTypes = ForgeTypesForRarity(forgeRegistrations, HextechRarityTier.Prismatic);
			DisabledPlayerRuneTypes = RuneTypesWithFlag(runeRegistrations, RuneFlags.Disabled).ToHashSet();
			IroncladRuneTypes = RuneTypesForCharacter(runeRegistrations, HextechCharacterPool.Ironclad);
			SilentRuneTypes = RuneTypesForCharacter(runeRegistrations, HextechCharacterPool.Silent);
			RegentRuneTypes = RuneTypesForCharacter(runeRegistrations, HextechCharacterPool.Regent);
			DefectRuneTypes = RuneTypesForCharacter(runeRegistrations, HextechCharacterPool.Defect);
			NecrobinderRuneTypes = RuneTypesForCharacter(runeRegistrations, HextechCharacterPool.Necrobinder);
			AttributeConversionExclusiveRuneTypes = RuneTypesWithFlag(runeRegistrations, RuneFlags.AttributeConversionExclusive);
			FirstActExcludedRuneTypes = RuneTypesWithFlag(runeRegistrations, RuneFlags.FirstActExcluded).ToHashSet();
			ThirdActExcludedRuneTypes = RuneTypesWithFlag(runeRegistrations, RuneFlags.ThirdActExcluded).ToHashSet();
			DisabledMonsterHexes = monsterHexRegistrations
				.Where(static registration => registration.Disabled)
				.Select(static registration => registration.Kind)
				.ToHashSet();
			MonsterHexesWithBurnHoverTip = monsterHexRegistrations
				.Where(static registration => registration.HasBurnHoverTip)
				.Select(static registration => registration.Kind)
				.ToHashSet();
			MonsterHexIconRelicTypes = monsterHexRegistrations
				.ToDictionary(static registration => registration.Kind, static registration => registration.IconRelicType);
			AllMonsterHexKinds = monsterHexRegistrations
				.Select(static registration => registration.Kind)
				.ToHashSet();
			SilverMonsterHexes = MonsterHexesForRarity(monsterHexRegistrations, HextechRarityTier.Silver);
			GoldMonsterHexes = MonsterHexesForRarity(monsterHexRegistrations, HextechRarityTier.Gold);
			PrismaticMonsterHexes = MonsterHexesForRarity(monsterHexRegistrations, HextechRarityTier.Prismatic);
			AllRuneTypes = SilverRuneTypes
				.Concat(GoldRuneTypes)
				.Concat(PrismaticRuneTypes)
				.Distinct()
				.ToArray();
			AllForgeTypes = SilverForgeTypes
				.Concat(GoldForgeTypes)
				.Concat(PrismaticForgeTypes)
				.Distinct()
				.ToArray();
			AllCustomRelicTypes = AllRuneTypes
				.Concat(AllForgeTypes)
				.Concat(shopOnlyRelicTypes)
				.Distinct()
				.ToArray();
		}

		public IReadOnlyList<Type> SilverRuneTypes { get; }
		public IReadOnlyList<Type> GoldRuneTypes { get; }
		public IReadOnlyList<Type> PrismaticRuneTypes { get; }
		public IReadOnlyList<Type> SilverForgeTypes { get; }
		public IReadOnlyList<Type> GoldForgeTypes { get; }
		public IReadOnlyList<Type> PrismaticForgeTypes { get; }
		public IReadOnlySet<Type> DisabledPlayerRuneTypes { get; }
		public IReadOnlyList<Type> IroncladRuneTypes { get; }
		public IReadOnlyList<Type> SilentRuneTypes { get; }
		public IReadOnlyList<Type> RegentRuneTypes { get; }
		public IReadOnlyList<Type> DefectRuneTypes { get; }
		public IReadOnlyList<Type> NecrobinderRuneTypes { get; }
		public IReadOnlyList<Type> AttributeConversionExclusiveRuneTypes { get; }
		public IReadOnlySet<Type> FirstActExcludedRuneTypes { get; }
		public IReadOnlySet<Type> ThirdActExcludedRuneTypes { get; }
		public IReadOnlySet<MonsterHexKind> DisabledMonsterHexes { get; }
		public IReadOnlySet<MonsterHexKind> MonsterHexesWithBurnHoverTip { get; }
		public IReadOnlyDictionary<MonsterHexKind, Type> MonsterHexIconRelicTypes { get; }
		public IReadOnlySet<MonsterHexKind> AllMonsterHexKinds { get; }
		public IReadOnlyList<MonsterHexKind> SilverMonsterHexes { get; }
		public IReadOnlyList<MonsterHexKind> GoldMonsterHexes { get; }
		public IReadOnlyList<MonsterHexKind> PrismaticMonsterHexes { get; }
		public IReadOnlyList<Type> AllRuneTypes { get; }
		public IReadOnlyList<Type> AllForgeTypes { get; }
		public IReadOnlyList<Type> AllCustomRelicTypes { get; }

		private static IReadOnlyList<Type> RuneTypesForRarity(IReadOnlyList<RuneRegistration> registrations, HextechRarityTier rarity)
		{
			return registrations
				.Where(registration => registration.Rarity == rarity)
				.Select(static registration => registration.Type)
				.ToArray();
		}

		private static IReadOnlyList<Type> ForgeTypesForRarity(IReadOnlyList<ForgeRegistration> registrations, HextechRarityTier rarity)
		{
			return registrations
				.Where(registration => registration.Rarity == rarity)
				.Select(static registration => registration.Type)
				.ToArray();
		}

		private static IReadOnlyList<Type> RuneTypesForCharacter(IReadOnlyList<RuneRegistration> registrations, HextechCharacterPool characterPool)
		{
			return registrations
				.Where(registration => registration.CharacterPool == characterPool)
				.OrderBy(static registration => registration.CharacterOrder)
				.Select(static registration => registration.Type)
				.ToArray();
		}

		private static IReadOnlyList<Type> RuneTypesWithFlag(IReadOnlyList<RuneRegistration> registrations, RuneFlags flag)
		{
			return registrations
				.Where(registration => (registration.Flags & flag) != 0)
				.Select(static registration => registration.Type)
				.ToArray();
		}

		private static IReadOnlyList<MonsterHexKind> MonsterHexesForRarity(IReadOnlyList<MonsterHexRegistration> registrations, HextechRarityTier rarity)
		{
			return registrations
				.Where(registration => registration.Rarity == rarity && !registration.Disabled)
				.Select(static registration => registration.Kind)
				.ToArray();
		}
	}
}
