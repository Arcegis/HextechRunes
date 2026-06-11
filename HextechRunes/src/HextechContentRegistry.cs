namespace HextechRunes;

internal static class HextechContentRegistry
{
	private static readonly Lazy<RegistryLookups> LazyLookups = new(BuildRegistryLookups);

	private static RegistryLookups Lookups => LazyLookups.Value;

	internal static IReadOnlyList<Type> SilverRuneTypes => Lookups.SilverRuneTypes;

	internal static IReadOnlyList<Type> GoldRuneTypes => Lookups.GoldRuneTypes;

	internal static IReadOnlyList<Type> PrismaticRuneTypes => Lookups.PrismaticRuneTypes;

	internal static IReadOnlyList<Type> SilverForgeTypes => Lookups.SilverForgeTypes;

	internal static IReadOnlyList<Type> GoldForgeTypes => Lookups.GoldForgeTypes;

	internal static IReadOnlyList<Type> PrismaticForgeTypes => Lookups.PrismaticForgeTypes;

	internal static IReadOnlySet<Type> DisabledPlayerRuneTypes => Lookups.DisabledPlayerRuneTypes;

	internal static IReadOnlySet<Type> SelectionExcludedPlayerRuneTypes => Lookups.SelectionExcludedPlayerRuneTypes;

	internal static IReadOnlyList<Type> IroncladRuneTypes => Lookups.IroncladRuneTypes;

	internal static IReadOnlyList<Type> SilentRuneTypes => Lookups.SilentRuneTypes;

	internal static IReadOnlyList<Type> RegentRuneTypes => Lookups.RegentRuneTypes;

	internal static IReadOnlyList<Type> DefectRuneTypes => Lookups.DefectRuneTypes;

	internal static IReadOnlyList<Type> NecrobinderRuneTypes => Lookups.NecrobinderRuneTypes;

	internal static IReadOnlyList<Type> AttributeConversionExclusiveRuneTypes => Lookups.AttributeConversionExclusiveRuneTypes;

	internal static IReadOnlyDictionary<Type, string> PlayerRuneTagKeys => Lookups.PlayerRuneTagKeys;

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

	internal static IReadOnlyList<Type> ShopOnlyRelicTypes => HextechCustomModelRegistry.ShopOnlyRelicTypes;

	internal static IReadOnlyList<Type> EventRelicTypes => HextechCustomModelRegistry.EventRelicTypes;

	internal static IReadOnlyList<Type> CustomCardTypes => HextechCustomModelRegistry.CustomCardTypes;

	private static RegistryLookups BuildRegistryLookups()
	{
		return new RegistryLookups(
			HextechPlayerRuneRegistry.Registrations,
			HextechForgeRegistry.Registrations,
			HextechMonsterHexRegistry.Registrations,
			HextechCustomModelRegistry.ShopOnlyRelicTypes);
	}

	private sealed class RegistryLookups
	{
		public RegistryLookups(
			IReadOnlyList<PlayerRuneRegistration> runeRegistrations,
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
			DisabledPlayerRuneTypes = RuneTypesWithFlag(runeRegistrations, PlayerRuneFlags.Disabled).ToHashSet();
			SelectionExcludedPlayerRuneTypes = RuneTypesWithFlag(runeRegistrations, PlayerRuneFlags.SelectionExcluded).ToHashSet();
			IroncladRuneTypes = RuneTypesForCharacter(runeRegistrations, PlayerRuneCharacterPool.Ironclad);
			SilentRuneTypes = RuneTypesForCharacter(runeRegistrations, PlayerRuneCharacterPool.Silent);
			RegentRuneTypes = RuneTypesForCharacter(runeRegistrations, PlayerRuneCharacterPool.Regent);
			DefectRuneTypes = RuneTypesForCharacter(runeRegistrations, PlayerRuneCharacterPool.Defect);
			NecrobinderRuneTypes = RuneTypesForCharacter(runeRegistrations, PlayerRuneCharacterPool.Necrobinder);
			AttributeConversionExclusiveRuneTypes = RuneTypesWithFlag(runeRegistrations, PlayerRuneFlags.AttributeConversionExclusive);
			PlayerRuneTagKeys = runeRegistrations
				.ToDictionary(static registration => registration.Type, static registration => registration.TagKey);
			FirstActExcludedRuneTypes = RuneTypesWithFlag(runeRegistrations, PlayerRuneFlags.FirstActExcluded).ToHashSet();
			ThirdActExcludedRuneTypes = RuneTypesWithFlag(runeRegistrations, PlayerRuneFlags.ThirdActExcluded).ToHashSet();
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
		public IReadOnlySet<Type> SelectionExcludedPlayerRuneTypes { get; }
		public IReadOnlyList<Type> IroncladRuneTypes { get; }
		public IReadOnlyList<Type> SilentRuneTypes { get; }
		public IReadOnlyList<Type> RegentRuneTypes { get; }
		public IReadOnlyList<Type> DefectRuneTypes { get; }
		public IReadOnlyList<Type> NecrobinderRuneTypes { get; }
		public IReadOnlyList<Type> AttributeConversionExclusiveRuneTypes { get; }
		public IReadOnlyDictionary<Type, string> PlayerRuneTagKeys { get; }
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

		private static IReadOnlyList<Type> RuneTypesForRarity(IReadOnlyList<PlayerRuneRegistration> registrations, HextechRarityTier rarity)
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

		private static IReadOnlyList<Type> RuneTypesForCharacter(IReadOnlyList<PlayerRuneRegistration> registrations, PlayerRuneCharacterPool characterPool)
		{
			return registrations
				.Where(registration => registration.CharacterPool == characterPool)
				.OrderBy(static registration => registration.CharacterOrder)
				.Select(static registration => registration.Type)
				.ToArray();
		}

		private static IReadOnlyList<Type> RuneTypesWithFlag(IReadOnlyList<PlayerRuneRegistration> registrations, PlayerRuneFlags flag)
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
