using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechModelBootstrap
{
	private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly FieldInfo? ModdedContentForPoolsField =
		typeof(ModHelper).GetField("_moddedContentForPools", BindingFlags.NonPublic | BindingFlags.Static);

	private static readonly object InstallLock = new();
	private static bool _installed;

	public static void Install()
	{
		lock (InstallLock)
		{
			if (_installed)
			{
				Log.Info($"[{ModInfo.Id}] Model bootstrap already installed; skipping duplicate registration.");
				return;
			}

			PreloadDependencyAssemblies();
			InjectSavedPropertyCaches();
			RegisterModels();
			_installed = true;
		}
	}

	private static void InjectSavedPropertyCaches()
	{
		foreach (Type type in HextechCatalog.GetAllCustomRelicTypes())
		{
			SavedPropertiesTypeCache.InjectTypeIntoCache(type);
		}

		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechMayhemModifier));
		foreach (Type type in HextechCustomRunModifierHooks.CustomRarityModifierTypes)
		{
			SavedPropertiesTypeCache.InjectTypeIntoCache(type);
		}

		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechBurnPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechTemporaryStrengthPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechTemporaryDexterityPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechTemporaryStrengthLossPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechTemporaryDexterityLossPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechLethalTempoTemporaryStrengthPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechBloodPactTemporaryStrengthPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechPowerShieldTemporaryStrengthPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechAttackReplayPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechTemporarySlowPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechOceanDragonSoulPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechInfernalDragonSoulPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechDragonSoulPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechMountainDragonSoulPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechChemtechDragonSoulPower));
		SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HextechCloudDragonSoulPower));
		EnsureSavedPropertyNetIdBitSize();
	}

	private static void EnsureSavedPropertyNetIdBitSize()
	{
		const int minimumBitSize = 16;
		const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;

		FieldInfo? mapField = TryGetField(typeof(SavedPropertiesTypeCache), "_netIdToPropertyNameMap", flags);
		int propertyNameCount = (mapField?.GetValue(null) as System.Collections.ICollection)?.Count ?? 0;
		int requiredBitSize = GetRequiredBitSize(propertyNameCount);
		int targetBitSize = Math.Max(minimumBitSize, requiredBitSize);
		int currentBitSize = SavedPropertiesTypeCache.NetIdBitSize;
		if (currentBitSize >= targetBitSize)
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] SavedPropertiesTypeCache NetIdBitSize unchanged: bitSize={currentBitSize} propertyNames={propertyNameCount}");
			return;
		}

		FieldInfo? backingField = TryGetField(typeof(SavedPropertiesTypeCache), "<NetIdBitSize>k__BackingField", flags);
		if (backingField == null)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] SavedPropertiesTypeCache NetIdBitSize backing field not found; custom saved properties may desync in multiplayer.");
			return;
		}

		backingField.SetValue(null, targetBitSize);
		Log.Info($"[{ModInfo.Id}][Mayhem] SavedPropertiesTypeCache NetIdBitSize updated: old={currentBitSize} new={targetBitSize} propertyNames={propertyNameCount}");
	}

	private static int GetRequiredBitSize(int valueCount)
	{
		int maxValue = Math.Max(1, valueCount - 1);
		int bits = 0;
		while (maxValue > 0)
		{
			bits++;
			maxValue >>= 1;
		}

		return bits;
	}

	private static void RegisterModels()
	{
		foreach (Type runeType in HextechCatalog.GetAllCustomRelicTypes())
		{
			TryAddModelToPool(typeof(SharedRelicPool), runeType);
		}

		foreach (Type cardType in HextechCatalog.GetAllCustomCardTypes())
		{
			TryAddModelToPool(typeof(TokenCardPool), cardType);
		}
	}

	private static void TryAddModelToPool(Type poolType, Type modelType)
	{
		if (IsModelAlreadyQueuedForPool(poolType, modelType))
		{
			Log.Info($"[{ModInfo.Id}] Skipping duplicate pool registration for {modelType.FullName} in {poolType.FullName}.");
			return;
		}

		ModHelper.AddModelToPool(poolType, modelType);
	}

	private static bool IsModelAlreadyQueuedForPool(Type poolType, Type modelType)
	{
		try
		{
			if (ModdedContentForPoolsField?.GetValue(null) is not IDictionary pools)
			{
				return false;
			}

			foreach (DictionaryEntry entry in pools)
			{
				if (entry.Key is Type existingPoolType
					&& IsSameModelType(existingPoolType, poolType)
					&& ContentContainsModelType(entry.Value, modelType))
				{
					return true;
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}] Could not inspect existing mod pool registrations: {ex.GetType().Name}: {ex.Message}");
		}

		return false;
	}

	private static bool ContentContainsModelType(object? content, Type modelType)
	{
		if (content == null)
		{
			return false;
		}

		FieldInfo? modelsField = content.GetType().GetField("modelsToAdd", InstanceFields);
		if (modelsField?.GetValue(content) is not IEnumerable models)
		{
			return false;
		}

		foreach (object? model in models)
		{
			if (model is Type existingModelType && IsSameModelType(existingModelType, modelType))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsSameModelType(Type left, Type right)
	{
		return left == right
			|| (string.Equals(left.FullName, right.FullName, StringComparison.Ordinal)
				&& string.Equals(left.Assembly.GetName().Name, right.Assembly.GetName().Name, StringComparison.Ordinal));
	}

	private static void PreloadDependencyAssemblies()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		string? modDirectory = Path.GetDirectoryName(assembly.Location);
		if (string.IsNullOrEmpty(modDirectory) || !Directory.Exists(modDirectory))
		{
			return;
		}

		string selfPath = assembly.Location;
		AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(assembly) ?? AssemblyLoadContext.Default;
		foreach (string dllPath in Directory.GetFiles(modDirectory, "*.dll"))
		{
			if (string.Equals(dllPath, selfPath, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			loadContext.LoadFromAssemblyPath(dllPath);
		}
	}
}
