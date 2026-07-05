using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static partial class HextechCatalog
{
	public readonly record struct RuneSeriesGroup(string LocalizationKey, IReadOnlyList<RelicModel> Relics);

	private readonly record struct CharacterRunePool(string LocalizationKey, IReadOnlyList<Type> RuneTypes);

	private static PlayerRuneMetadataCatalog PlayerRuneMetadata => HextechContentRegistry.PlayerRuneMetadata;

	private static IReadOnlyList<Type> SilverRuneTypes => PlayerRuneMetadata.TypesByRarity[HextechRarityTier.Silver];

	private static IReadOnlyList<Type> GoldRuneTypes => PlayerRuneMetadata.TypesByRarity[HextechRarityTier.Gold];

	private static IReadOnlyList<Type> PrismaticRuneTypes => PlayerRuneMetadata.TypesByRarity[HextechRarityTier.Prismatic];

	private static IReadOnlyList<Type> SilverForgeTypes => HextechContentRegistry.SilverForgeTypes;

	private static IReadOnlyList<Type> GoldForgeTypes => HextechContentRegistry.GoldForgeTypes;

	private static IReadOnlyList<Type> PrismaticForgeTypes => HextechContentRegistry.PrismaticForgeTypes;

	private static IReadOnlyList<Type> ShopOnlyRelicTypes => HextechContentRegistry.ShopOnlyRelicTypes;

	private static IReadOnlyList<CharacterRunePool> CharacterRunePools =>
	[
		new("IRONCLAD", PlayerRuneMetadata.TypesByCharacter[PlayerRuneCharacterPool.Ironclad]),
		new("SILENT", PlayerRuneMetadata.TypesByCharacter[PlayerRuneCharacterPool.Silent]),
		new("REGENT", PlayerRuneMetadata.TypesByCharacter[PlayerRuneCharacterPool.Regent]),
		new("DEFECT", PlayerRuneMetadata.TypesByCharacter[PlayerRuneCharacterPool.Defect]),
		new("NECROBINDER", PlayerRuneMetadata.TypesByCharacter[PlayerRuneCharacterPool.Necrobinder])
	];

	private static IReadOnlySet<Type> CharacterSpecificRuneTypes => PlayerRuneMetadata.GetCharacterSpecificTypes();

	private static IReadOnlyList<Type> AttributeConversionExclusiveRuneTypes =>
		PlayerRuneMetadata.TypesByFlag[PlayerRuneFlags.AttributeConversionExclusive];

	private static IReadOnlyList<Type> AllRuneTypes => PlayerRuneMetadata.AllTypes;

	private static IReadOnlyList<Type> AllForgeTypes => HextechContentRegistry.AllForgeTypes;

	private static IReadOnlyList<Type> AllCustomRelicTypes => HextechContentRegistry.AllCustomRelicTypes;

	private static IReadOnlyList<Type> CustomCardTypes => HextechContentRegistry.CustomCardTypes;

	public static IReadOnlyList<Type> GetAllRuneTypes() => AllRuneTypes;

	public static IReadOnlyList<Type> GetAllSelectableRuneTypes()
	{
		return Enum.GetValues<HextechRarityTier>()
			.SelectMany(GetPlayerRuneTypesForRarity)
			.ToArray();
	}

	public static IReadOnlyList<Type> GetAllConfigurableRuneTypes()
	{
		return Enum.GetValues<HextechRarityTier>()
			.SelectMany(GetConfigurablePlayerRuneTypesForRarity)
			.ToArray();
	}

	public static bool IsPlayerRuneTypeSelectable(Type runeType)
	{
		return PlayerRuneMetadata.IsSelectable(runeType);
	}

	public static bool IsPlayerRuneTypeConfigurable(Type runeType)
	{
		return PlayerRuneMetadata.IsConfigurable(runeType);
	}

	public static bool IsPlayerRuneTypeVisible(Type runeType)
	{
		return PlayerRuneMetadata.IsVisible(runeType);
	}

	public static bool IsPlayerRuneTypeVisibleInCollection(Type runeType)
	{
		if (!AllRuneTypes.Contains(runeType))
		{
			return false;
		}

		if (IsPlayerRuneTypeConfigurable(runeType))
		{
			return HextechRuneConfiguration.IsPlayerRuneEnabled(ModelDb.GetId(runeType).Entry);
		}

		return IsPlayerRuneTypeVisible(runeType);
	}

	public static IReadOnlyList<Type> GetGenericSelectableRuneTypes()
	{
		return GetAllSelectableRuneTypes()
			.Where(static type => !CharacterSpecificRuneTypes.Contains(type))
			.ToArray();
	}

	public static IReadOnlyList<Type> GetGenericVisibleRuneTypes()
	{
		return AllRuneTypes
			.Where(IsPlayerRuneTypeVisibleInCollection)
			.Where(static type => !CharacterSpecificRuneTypes.Contains(type))
			.ToArray();
	}

	public static string GetPlayerRunePoolKey(RelicModel relic)
	{
		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		foreach (CharacterRunePool pool in CharacterRunePools)
		{
			foreach (Type runeType in pool.RuneTypes)
			{
				if (ModelDb.GetId(runeType) == id)
				{
					return pool.LocalizationKey;
				}
			}
		}

		// 外部模组(拓展包)经 HextechRunesApi 注册的符文,池标签显示"拓展包"而非"通用"。
		if (HextechExternalContentRegistry.GetAssetModId(id) != null)
		{
			return "SPONSOR_PACK";
		}

		return "GENERIC";
	}

	public static IReadOnlyList<Type> GetAllForgeTypes() => AllForgeTypes;

	public static IReadOnlyList<Type> GetAllCustomRelicTypes() => AllCustomRelicTypes;

	public static IReadOnlyList<Type> GetAllCustomCardTypes() => CustomCardTypes;

	public static IReadOnlyList<Type> GetPlayerRuneTypesForRarity(HextechRarityTier rarity)
	{
		return PlayerRuneMetadata.GetSelectableTypesForRarity(rarity);
	}

	public static IReadOnlyList<Type> GetConfigurablePlayerRuneTypesForRarity(HextechRarityTier rarity)
	{
		return PlayerRuneMetadata.GetConfigurableTypesForRarity(rarity);
	}

	public static IReadOnlySet<ModelId> GetConfigurablePlayerRuneIds()
	{
		return GetAllConfigurableRuneTypes()
			.Select(ModelDb.GetId)
			.ToHashSet();
	}

	public static IReadOnlySet<ModelId> GetDefaultDisabledPlayerRuneIds()
	{
		return PlayerRuneMetadata.TypesByFlag[PlayerRuneFlags.Disabled]
			.Where(IsPlayerRuneTypeConfigurable)
			.Select(ModelDb.GetId)
			.ToHashSet();
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

	public static bool IsAvailableForPlayer(RelicModel relic, Player player)
	{
		return relic is not HextechRelicBase hextechRelic || hextechRelic.IsAvailableForPlayer(player);
	}

	public static bool IsPlayerRuneAllowedInAct(Type runeType, int actIndex)
	{
		return actIndex switch
		{
			0 => !PlayerRuneMetadata.HasFlag(runeType, PlayerRuneFlags.FirstActExcluded),
			2 => IsEndlessModeLoaded() || !PlayerRuneMetadata.HasFlag(runeType, PlayerRuneFlags.ThirdActExcluded),
			_ => true
		};
	}

	private static bool IsEndlessModeLoaded()
	{
		foreach (object mod in EnumerateKnownMods())
		{
			if (IsLoadedEndlessModeMod(mod))
			{
				return true;
			}
		}

		return false;
	}

	private static IEnumerable<object> EnumerateKnownMods()
	{
		Type modManagerType = typeof(ModManager);
		object? mods =
			modManagerType.GetProperty("LoadedMods", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
			?? modManagerType.GetField("_loadedMods", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
			?? modManagerType.GetProperty("AllMods", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
			?? modManagerType.GetField("_mods", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);

		if (mods is not IEnumerable enumerable)
		{
			yield break;
		}

		foreach (object? mod in enumerable)
		{
			if (mod != null)
			{
				yield return mod;
			}
		}
	}

	private static bool IsLoadedEndlessModeMod(object mod)
	{
		Type modType = mod.GetType();
		object? manifest = modType.GetField("manifest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(mod)
			?? modType.GetProperty("manifest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(mod);
		string? id = manifest?.GetType().GetField("id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(manifest) as string
			?? manifest?.GetType().GetProperty("id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(manifest) as string;
		if (!string.Equals(id, "EndlessMode", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		object? wasLoadedValue = modType.GetField("wasLoaded", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(mod)
			?? modType.GetProperty("wasLoaded", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(mod);
		return wasLoadedValue is not bool wasLoaded || wasLoaded;
	}
}
