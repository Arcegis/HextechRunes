namespace HextechRunes;

internal static class HextechExternalContentRegistry
{
	private static readonly object SyncRoot = new();
	private static readonly List<PlayerRuneRegistration> PlayerRuneRegistrations = new();
	private static readonly List<ForgeRegistration> ForgeRegistrations = new();
	private static readonly List<Type> EventRelicTypes = new();
	private static readonly Dictionary<ModelId, string> AssetModIdsByModelId = new();
	private static readonly Dictionary<ModelId, string> EnchantmentIconPathsByModelId = new();
	private static int _version;

	internal static int Version
	{
		get
		{
			lock (SyncRoot)
			{
				return _version;
			}
		}
	}

	internal static void RegisterPlayerRune(PlayerRuneRegistration registration, string? assetModId)
	{
		lock (SyncRoot)
		{
			if (!PlayerRuneRegistrations.Any(existing => HextechModelTypeIdentity.IsSame(existing.Type, registration.Type)))
			{
				PlayerRuneRegistrations.Add(registration);
				StoreAssetModId(registration.Type, assetModId);
				_version++;
				return;
			}

			StoreAssetModId(registration.Type, assetModId);
		}
	}

	internal static void RegisterEventRelic(Type relicType, string? assetModId)
	{
		lock (SyncRoot)
		{
			if (!EventRelicTypes.Any(existing => HextechModelTypeIdentity.IsSame(existing, relicType)))
			{
				EventRelicTypes.Add(relicType);
				StoreAssetModId(relicType, assetModId);
				_version++;
				return;
			}

			StoreAssetModId(relicType, assetModId);
		}
	}

	internal static void RegisterForge(ForgeRegistration registration, string? assetModId)
	{
		lock (SyncRoot)
		{
			if (!ForgeRegistrations.Any(existing => HextechModelTypeIdentity.IsSame(existing.Type, registration.Type)))
			{
				ForgeRegistrations.Add(registration);
				StoreAssetModId(registration.Type, assetModId);
				_version++;
				return;
			}

			StoreAssetModId(registration.Type, assetModId);
		}
	}

	internal static void RegisterEnchantmentIcon(Type enchantmentType, string iconPath)
	{
		lock (SyncRoot)
		{
			EnchantmentIconPathsByModelId[ModelDb.GetId(enchantmentType)] = iconPath;
			_version++;
		}
	}

	internal static IReadOnlyList<PlayerRuneRegistration> GetPlayerRuneRegistrations()
	{
		lock (SyncRoot)
		{
			return PlayerRuneRegistrations.ToArray();
		}
	}

	internal static IReadOnlyList<Type> GetEventRelicTypes()
	{
		lock (SyncRoot)
		{
			return EventRelicTypes.ToArray();
		}
	}

	internal static IReadOnlyList<ForgeRegistration> GetForgeRegistrations()
	{
		lock (SyncRoot)
		{
			return ForgeRegistrations.ToArray();
		}
	}

	internal static string? GetAssetModId(ModelId id)
	{
		lock (SyncRoot)
		{
			return AssetModIdsByModelId.TryGetValue(id, out string? modId)
				? modId
					: null;
		}
	}

	internal static string? GetEnchantmentIconPath(ModelId id)
	{
		lock (SyncRoot)
		{
			return EnchantmentIconPathsByModelId.TryGetValue(id, out string? path)
				? path
				: null;
		}
	}

	private static void StoreAssetModId(Type modelType, string? assetModId)
	{
		if (string.IsNullOrWhiteSpace(assetModId))
		{
			return;
		}

		AssetModIdsByModelId[ModelDb.GetId(modelType)] = assetModId;
	}
}
