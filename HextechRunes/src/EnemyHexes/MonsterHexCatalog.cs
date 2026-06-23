using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static class MonsterHexCatalog
{
	private static readonly IReadOnlyList<MonsterHexKind> SilverMonsterHexes = HextechContentRegistry.SilverMonsterHexes;

	private static readonly IReadOnlyList<MonsterHexKind> GoldMonsterHexes = HextechContentRegistry.GoldMonsterHexes;

	private static readonly IReadOnlyList<MonsterHexKind> PrismaticMonsterHexes = HextechContentRegistry.PrismaticMonsterHexes;

	private static readonly IReadOnlyDictionary<MonsterHexKind, Type> MonsterHexIconRelicTypes = HextechContentRegistry.MonsterHexIconRelicTypes;

	private static readonly IReadOnlySet<MonsterHexKind> EnemyHexesWithBurnHoverTip =
		HextechContentRegistry.MonsterHexesWithBurnHoverTip;

	private static readonly Lazy<IReadOnlyDictionary<MonsterHexKind, HextechRarityTier>> RarityByMonsterHex = new(BuildRarityByMonsterHex);

	private static readonly Lazy<IReadOnlyDictionary<ModelId, MonsterHexKind>> MonsterHexByIconRelicId = new(BuildMonsterHexByIconRelicId);

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
		if (RarityByMonsterHex.Value.TryGetValue(hex, out HextechRarityTier rarity))
		{
			return rarity;
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
		return MonsterHexByIconRelicId.Value.TryGetValue(id, out hex);
	}

	public static string GetEnemyHexDescriptionFormatted(MonsterHexKind hex)
	{
		RelicModel relic = GetIconRelicForMonsterHex(hex);
		string localizationKey = GetEnemyHexDescriptionKey(relic);
		try
		{
			return new LocString("relics", localizationKey).GetFormattedText();
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Enemy hex description fallback: hex={hex} key={localizationKey} error={ex.Message}");
			try
			{
				return relic.DynamicDescription.GetFormattedText();
			}
			catch (Exception fallbackEx)
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem] Enemy hex description fallback failed: hex={hex} relic={(relic.CanonicalInstance?.Id ?? relic.Id).Entry} error={fallbackEx.Message}");
				return relic.Title.GetFormattedText();
			}
		}
	}

	public static IEnumerable<IHoverTip> GetEnemyHexHoverTips(MonsterHexKind hex)
	{
		RelicModel relic = GetIconRelicForMonsterHex(hex);
		HoverTip mainTip = new(relic.Title, GetEnemyHexDescriptionFormatted(hex), GetEnemyHexHoverIcon(relic) ?? relic.Icon);
		if (EnemyHexesWithBurnHoverTip.Contains(hex))
		{
			return [mainTip, HoverTipFactory.FromPower<HextechBurnPower>()];
		}

		if (hex == MonsterHexKind.Compensation)
		{
			return [mainTip, HoverTipFactory.FromPower<PoisonPower>()];
		}

		return [mainTip];
	}

	private static Texture2D? GetEnemyHexHoverIcon(RelicModel relic)
	{
		string? path = HextechAssets.TryGetCustomRelicIconPath(relic);
		return path == null ? null : AssetHooks.LoadUiTexture(path);
	}

	private static string GetEnemyHexDescriptionKey(RelicModel relic)
	{
		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		return HextechAssets.ToImageFileStem(id.Entry) + ".enemyDescription";
	}

	private static IReadOnlyDictionary<MonsterHexKind, HextechRarityTier> BuildRarityByMonsterHex()
	{
		Dictionary<MonsterHexKind, HextechRarityTier> byHex = new();
		AddRarityEntries(byHex, SilverMonsterHexes, HextechRarityTier.Silver);
		AddRarityEntries(byHex, GoldMonsterHexes, HextechRarityTier.Gold);
		AddRarityEntries(byHex, PrismaticMonsterHexes, HextechRarityTier.Prismatic);
		return byHex;
	}

	private static void AddRarityEntries(
		Dictionary<MonsterHexKind, HextechRarityTier> byHex,
		IEnumerable<MonsterHexKind> hexes,
		HextechRarityTier rarity)
	{
		foreach (MonsterHexKind hex in hexes)
		{
			byHex[hex] = rarity;
		}
	}

	private static IReadOnlyDictionary<ModelId, MonsterHexKind> BuildMonsterHexByIconRelicId()
	{
		Dictionary<ModelId, MonsterHexKind> byId = new();
		foreach (KeyValuePair<MonsterHexKind, Type> pair in MonsterHexIconRelicTypes)
		{
			RelicModel iconRelic = ModelDb.GetById<RelicModel>(ModelDb.GetId(pair.Value));
			ModelId id = iconRelic.CanonicalInstance?.Id ?? iconRelic.Id;
			byId[id] = pair.Key;
		}

		return byId;
	}
}
