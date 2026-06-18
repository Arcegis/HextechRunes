using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static partial class HextechCatalog
{
	private static readonly Lazy<IReadOnlySet<ModelId>> RuneIds = new(() => ToModelIdSet(AllRuneTypes));

	private static readonly Lazy<IReadOnlySet<ModelId>> ForgeIds = new(() => ToModelIdSet(AllForgeTypes));

	private static readonly Lazy<IReadOnlySet<ModelId>> ShopOnlyRelicIds = new(() => ToModelIdSet(ShopOnlyRelicTypes));

	private static readonly Lazy<IReadOnlyDictionary<ModelId, HextechRarityTier>> PlayerRuneRarityById = new(BuildPlayerRuneRarityById);

	private static readonly Lazy<IReadOnlyDictionary<ModelId, HextechRarityTier>> ForgeRarityById = new(BuildForgeRarityById);

	private static readonly Lazy<IReadOnlyDictionary<ModelId, string>> PlayerRuneTagKeyById = new(BuildPlayerRuneTagKeyById);

	private static readonly Lazy<IReadOnlySet<ModelId>> AttributeConversionExclusiveRuneIds =
		new(() => ToModelIdSet(PlayerRuneMetadata.TypesByFlag[PlayerRuneFlags.AttributeConversionExclusive]));

	public static bool IsHextechRelic(RelicModel? relic)
	{
		if (relic == null)
		{
			return false;
		}

		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		return RuneIds.Value.Contains(id);
	}

	public static bool IsHextechForgeRelic(RelicModel? relic)
	{
		if (relic == null)
		{
			return false;
		}

		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		return ForgeIds.Value.Contains(id);
	}

	public static bool IsHextechShopRelic(RelicModel? relic)
	{
		if (relic == null)
		{
			return false;
		}

		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		return ShopOnlyRelicIds.Value.Contains(id);
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
		return PlayerRuneRarityById.Value.TryGetValue(id, out rarity);
	}

	public static bool TryGetForgeRarity(RelicModel? relic, out HextechRarityTier rarity)
	{
		rarity = default;
		if (relic == null)
		{
			return false;
		}

		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		return ForgeRarityById.Value.TryGetValue(id, out rarity);
	}

	public static string GetPlayerRuneTagKey(RelicModel relic)
	{
		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		return PlayerRuneTagKeyById.Value.TryGetValue(id, out string? tagKey)
			? tagKey
			: HextechPlayerRuneRegistry.DefaultTagKey;
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

	private static bool IsAttributeConversionExclusiveRuneId(ModelId id)
	{
		return AttributeConversionExclusiveRuneIds.Value.Contains(id);
	}

	private static IReadOnlySet<ModelId> ToModelIdSet(IEnumerable<Type> modelTypes)
	{
		return modelTypes.Select(ModelDb.GetId).ToHashSet();
	}

	private static IReadOnlyDictionary<ModelId, HextechRarityTier> BuildPlayerRuneRarityById()
	{
		Dictionary<ModelId, HextechRarityTier> byId = new();
		foreach (PlayerRuneRegistration registration in PlayerRuneMetadata.Registrations)
		{
			byId[ModelDb.GetId(registration.Type)] = registration.Rarity;
		}

		return byId;
	}

	private static IReadOnlyDictionary<ModelId, HextechRarityTier> BuildForgeRarityById()
	{
		Dictionary<ModelId, HextechRarityTier> byId = new();
		AddRarityEntries(byId, SilverForgeTypes, HextechRarityTier.Silver);
		AddRarityEntries(byId, GoldForgeTypes, HextechRarityTier.Gold);
		AddRarityEntries(byId, PrismaticForgeTypes, HextechRarityTier.Prismatic);
		return byId;
	}

	private static IReadOnlyDictionary<ModelId, string> BuildPlayerRuneTagKeyById()
	{
		Dictionary<ModelId, string> byId = new();
		foreach (PlayerRuneRegistration registration in PlayerRuneMetadata.Registrations)
		{
			byId[ModelDb.GetId(registration.Type)] = registration.TagKey;
		}

		return byId;
	}

	private static void AddRarityEntries(Dictionary<ModelId, HextechRarityTier> byId, IEnumerable<Type> modelTypes, HextechRarityTier rarity)
	{
		foreach (Type modelType in modelTypes)
		{
			byId[ModelDb.GetId(modelType)] = rarity;
		}
	}
}
