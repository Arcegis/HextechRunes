using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static partial class HextechCatalog
{
	private static readonly object ModelIdLookupLock = new();
	private static ModelIdLookupCache? _modelIdLookupCache;
	private static int _modelIdLookupCacheVersion = -1;

	public static bool IsHextechRelic(RelicModel? relic)
	{
		if (relic == null)
		{
			return false;
		}

		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		return ModelIdLookups.RuneIds.Contains(id);
	}

	public static bool IsHextechForgeRelic(RelicModel? relic)
	{
		if (relic == null)
		{
			return false;
		}

		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		return ModelIdLookups.ForgeIds.Contains(id);
	}

	public static bool IsHextechShopRelic(RelicModel? relic)
	{
		if (relic == null)
		{
			return false;
		}

		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		return ModelIdLookups.ShopOnlyRelicIds.Contains(id);
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
		return ModelIdLookups.PlayerRuneRarityById.TryGetValue(id, out rarity);
	}

	public static bool TryGetForgeRarity(RelicModel? relic, out HextechRarityTier rarity)
	{
		rarity = default;
		if (relic == null)
		{
			return false;
		}

		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		return ModelIdLookups.ForgeRarityById.TryGetValue(id, out rarity);
	}

	public static string GetPlayerRuneTagKey(RelicModel relic)
	{
		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		return ModelIdLookups.PlayerRuneTagKeyById.TryGetValue(id, out string? tagKey)
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
		return ModelIdLookups.AttributeConversionExclusiveRuneIds.Contains(id);
	}

	private static ModelIdLookupCache ModelIdLookups
	{
		get
		{
			int version = HextechContentRegistry.Version;
			lock (ModelIdLookupLock)
			{
				if (_modelIdLookupCache == null || _modelIdLookupCacheVersion != version)
				{
					_modelIdLookupCache = BuildModelIdLookupCache();
					_modelIdLookupCacheVersion = version;
				}

				return _modelIdLookupCache;
			}
		}
	}

	private static ModelIdLookupCache BuildModelIdLookupCache()
	{
		return new ModelIdLookupCache(
			ToModelIdSet(AllRuneTypes),
			ToModelIdSet(AllForgeTypes),
			ToModelIdSet(ShopOnlyRelicTypes),
			BuildPlayerRuneRarityById(),
			BuildForgeRarityById(),
			BuildPlayerRuneTagKeyById(),
			ToModelIdSet(PlayerRuneMetadata.TypesByFlag[PlayerRuneFlags.AttributeConversionExclusive]));
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

	private sealed record ModelIdLookupCache(
		IReadOnlySet<ModelId> RuneIds,
		IReadOnlySet<ModelId> ForgeIds,
		IReadOnlySet<ModelId> ShopOnlyRelicIds,
		IReadOnlyDictionary<ModelId, HextechRarityTier> PlayerRuneRarityById,
		IReadOnlyDictionary<ModelId, HextechRarityTier> ForgeRarityById,
		IReadOnlyDictionary<ModelId, string> PlayerRuneTagKeyById,
		IReadOnlySet<ModelId> AttributeConversionExclusiveRuneIds);
}
