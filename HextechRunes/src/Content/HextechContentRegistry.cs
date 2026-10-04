namespace HextechRunes;

internal static class HextechContentRegistry
{
	private static readonly object LookupsLock = new();
	private static RegistryLookups? _lookups;
	private static int _lookupsVersion = -1;

	internal static int Version => HextechExternalContentRegistry.Version;

	private static RegistryLookups Lookups
	{
		get
		{
			int version = Version;
			lock (LookupsLock)
			{
				if (_lookups == null || _lookupsVersion != version)
				{
					_lookups = BuildRegistryLookups();
					_lookupsVersion = version;
				}

				return _lookups;
			}
		}
	}

	internal static PlayerRuneMetadataCatalog PlayerRuneMetadata => Lookups.PlayerRuneMetadata;

	internal static MonsterHexMetadataCatalog MonsterHexMetadata => Lookups.MonsterHexMetadata;

	internal static IReadOnlyDictionary<HextechRarityTier, IReadOnlyList<Type>> ForgeTypesByRarity => Lookups.ForgeTypesByRarity;

	internal static IReadOnlyDictionary<Type, HextechRarityTier> ForgeRarityByType => Lookups.ForgeRarityByType;

	internal static IReadOnlyList<Type> AllForgeTypes => Lookups.AllForgeTypes;

	internal static IReadOnlyList<Type> AllCustomRelicTypes => Lookups.AllCustomRelicTypes;

	internal static IReadOnlyList<Type> EventRelicTypes =>
		HextechCustomModelRegistry.EventRelicTypes
			.Concat(HextechExternalContentRegistry.GetEventRelicTypes())
			.ToArray();

	private static RegistryLookups BuildRegistryLookups()
	{
		return new RegistryLookups(
				HextechPlayerRuneRegistry.Registrations
					.Concat(HextechExternalContentRegistry.GetPlayerRuneRegistrations())
					.ToArray(),
				HextechForgeRegistry.Registrations
					.Concat(HextechExternalContentRegistry.GetForgeRegistrations())
					.ToArray(),
				HextechMonsterHexRegistry.Registrations);
	}

	// 只做类型层面的派生，不调用 ModelDb：ModelId 的捕获放在 HextechCatalog.ModelIdLookups，两者的构建时机不能合并。
	private sealed class RegistryLookups
	{
		public RegistryLookups(
			IReadOnlyList<PlayerRuneRegistration> runeRegistrations,
			IReadOnlyList<ForgeRegistration> forgeRegistrations,
			IReadOnlyList<MonsterHexRegistration> monsterHexRegistrations)
		{
			PlayerRuneMetadata = new PlayerRuneMetadataCatalog(runeRegistrations);
			MonsterHexMetadata = new MonsterHexMetadataCatalog(monsterHexRegistrations);
			// ToDictionary 同时拒绝重复登记的锻造器类型。
			ForgeRarityByType = forgeRegistrations.ToDictionary(
				static registration => registration.Type,
				static registration => registration.Rarity);
			ForgeTypesByRarity = Enum.GetValues<HextechRarityTier>()
				.ToDictionary(
					static rarity => rarity,
					rarity => (IReadOnlyList<Type>)forgeRegistrations
						.Where(registration => registration.Rarity == rarity)
						.Select(static registration => registration.Type)
						.ToArray());
			AllForgeTypes = Enum.GetValues<HextechRarityTier>()
				.SelectMany(rarity => ForgeTypesByRarity[rarity])
				.ToArray();
			AllCustomRelicTypes = PlayerRuneMetadata.AllTypes
				.Concat(AllForgeTypes)
				.Concat(HextechCustomModelRegistry.ShopOnlyRelicTypes)
				.Concat(HextechCustomModelRegistry.EnemyHexIconRelicTypes)
				.Distinct()
				.ToArray();
		}

		public PlayerRuneMetadataCatalog PlayerRuneMetadata { get; }
		public MonsterHexMetadataCatalog MonsterHexMetadata { get; }
		public IReadOnlyDictionary<HextechRarityTier, IReadOnlyList<Type>> ForgeTypesByRarity { get; }
		public IReadOnlyDictionary<Type, HextechRarityTier> ForgeRarityByType { get; }
		public IReadOnlyList<Type> AllForgeTypes { get; }
		public IReadOnlyList<Type> AllCustomRelicTypes { get; }
	}
}
