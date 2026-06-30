using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static class HextechRuneConfiguration
{
	private const string ConfigFileName = "rune_config.json";
	private const int CurrentConfigVersion = 14;
	private const int HexActCount = 3;
	private const int MinActHexCount = 0;
	private const int MaxActHexCount = 6;
	public const int InfiniteRerollLimit = -1;
	private const int MinFiniteRerollLimit = 0;
	private const int MaxFiniteRerollLimit = 9;
	private const int MinRarityWeight = 0;
	private const int MaxRarityWeight = 999;
	private const int MinRandomForgeShopPrice = 0;
	private const int MaxRandomForgeShopPrice = 9999;
	private const int DefaultRandomForgeShopPrice = 250;
	private const bool DefaultRandomForgeDirectGrant = false;
	// 模组总开关默认开启:关闭后本局表现得与原版一致(开局时快照,联机按房主)。
	private const bool DefaultModEnabled = true;
	private static readonly int[] DefaultPlayerHexCountsByAct = [ 1, 1, 1 ];
	private static readonly int[] DefaultEnemyHexCountsByAct = [ 1, 2, 3 ];
	private const int DefaultPlayerRuneRerollLimit = 1;
	private const int DefaultMonsterHexRerollLimit = InfiniteRerollLimit;
	private static readonly int[] LegacyEnemyHexCountsDefault = [ 1, 2, 3 ];
	private static readonly int[] Version9EnemyHexCountsDefault = [ 1, 1, 1 ];
	private static readonly HextechRarityWeights DefaultFirstActRuneRarityWeights = new(2, 5, 3);
	// 配置版本 14 之前的第一幕默认值。比例与新默认 2-5-3 完全相同,仅用于把"从未自定义过"的旧配置迁到新默认。
	private static readonly HextechRarityWeights Version14LegacyFirstActRuneRarityWeights = new(20, 50, 30);
	private static readonly HextechRarityWeights DefaultNormalRuneRarityWeights = new(1, 1, 1);
	private static readonly HextechRarityWeights DefaultSecondActAfterSilverRuneRarityWeights = new(0, 1, 1);
	private static readonly HextechForgeRarityWeights DefaultForgeRarityWeights = new(65, 25, 10);
	private static readonly Type[] Version5DefaultDisabledRuneTypes =
	[
		typeof(DemonFormUpgradeRune),
		typeof(TyrannyUpgradeRune)
	];
	private static readonly Type[] Version6DefaultDisabledRuneTypes =
	[
		typeof(NeowsGrudgeRune),
		typeof(AnthonyBiasRune),
		typeof(CuttingEdgeAlchemistRune)
	];
	private static readonly Type[] Version7DefaultDisabledRuneTypes =
	[
		typeof(CrackTheEggRune)
	];
	private static readonly Type[] Version8DefaultDisabledRuneTypes =
	[
		typeof(EarthAwakensRune)
	];
	private static readonly Type[] Version8DefaultEnabledRuneTypes =
	[
		typeof(MikaelsBlessingRune)
	];
	private static readonly Type[] Version11DefaultDisabledRuneTypes =
	[
		typeof(HappyAccidentRune)
	];
	private static readonly Type[] Version13DefaultDisabledRuneTypes =
	[
		typeof(CorruptedBranchRune)
	];
	private static readonly Type[] Version13DefaultDisabledForgeTypes = [];

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true
	};

	private static readonly object SyncRoot = new();
	private static RuneConfig _config = new();
	private static bool _loaded;

	public static bool HasDisabledPlayerRunes
	{
		get
		{
			EnsureLoaded();
			lock (SyncRoot)
			{
				return _config.DisabledPlayerRuneIds.Count > 0;
			}
		}
	}

	public static void Initialize()
	{
		EnsureLoaded();
	}

	public static int[] GetEnemyHexCountsByAct()
	{
		EnsureLoaded();
		lock (SyncRoot)
		{
			return NormalizeEnemyHexCounts(_config.EnemyHexCountsByAct);
		}
	}

	public static int[] GetPlayerHexCountsByAct()
	{
		EnsureLoaded();
		lock (SyncRoot)
		{
			return NormalizePlayerHexCounts(_config.PlayerHexCountsByAct);
		}
	}

	public static bool IsPlayerRuneEnabled(RelicModel relic)
	{
		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		return IsPlayerRuneEnabled(id.Entry);
	}

	public static bool IsPlayerRuneEnabled(string id)
	{
		EnsureLoaded();
		lock (SyncRoot)
		{
			return !_config.DisabledPlayerRuneIds.Contains(id);
		}
	}

	public static IReadOnlySet<string> GetDisabledPlayerRuneIds()
	{
		EnsureLoaded();
		lock (SyncRoot)
		{
			return _config.DisabledPlayerRuneIds.ToHashSet(StringComparer.Ordinal);
		}
	}

	public static IReadOnlySet<string> GetDisabledMonsterHexIds()
	{
		EnsureLoaded();
		lock (SyncRoot)
		{
			return NormalizeDisabledMonsterHexIds(_config.DisabledMonsterHexIds);
		}
	}

	public static IReadOnlySet<string> GetDisabledForgeIds()
	{
		EnsureLoaded();
		lock (SyncRoot)
		{
			return NormalizeDisabledForgeIds(_config.DisabledForgeIds);
		}
	}

	public static HextechRunConfigurationSnapshot GetSnapshot()
	{
		EnsureLoaded();
		lock (SyncRoot)
		{
			return NormalizeSnapshot(new HextechRunConfigurationSnapshot(
				_config.PlayerHexCountsByAct ?? DefaultPlayerHexCountsByAct,
				_config.EnemyHexCountsByAct ?? DefaultEnemyHexCountsByAct,
				_config.PlayerRuneRerollLimit,
				_config.MonsterHexRerollLimit,
				_config.DisabledPlayerRuneIds,
				_config.DisabledMonsterHexIds,
				_config.DisabledForgeIds,
				ToRarityWeights(_config.FirstActRuneRarityWeights, DefaultFirstActRuneRarityWeights),
				ToRarityWeights(_config.NormalRuneRarityWeights, DefaultNormalRuneRarityWeights),
				ToRarityWeights(_config.SecondActAfterSilverRuneRarityWeights, DefaultSecondActAfterSilverRuneRarityWeights),
				ToForgeRarityWeights(_config.ForgeRarityWeights, DefaultForgeRarityWeights),
				_config.RandomForgeShopPrice,
				_config.RandomForgeDirectGrant,
				_config.ModEnabled));
		}
	}

	internal static HashSet<string> NormalizeDisabledPlayerRuneIds(IEnumerable<string>? ids)
	{
		return NormalizeConfigDisabledIds(ids);
	}

	internal static HashSet<string> NormalizeDisabledMonsterHexIds(IEnumerable<string>? ids)
	{
		HashSet<string> validIds = HextechContentRegistry.MonsterHexMetadata.EnabledKindsByRarity
			.Values
			.SelectMany(static kinds => kinds)
			.Select(static kind => kind.ToString())
			.ToHashSet(StringComparer.Ordinal);
		return NormalizeStringIds(ids, validIds);
	}

	internal static HashSet<string> NormalizeDisabledForgeIds(IEnumerable<string>? ids)
	{
		return NormalizeConfigStringIds(ids);
	}

	public static IReadOnlySet<string> GetDefaultDisabledPlayerRuneIds()
	{
		return HextechCatalog.GetDefaultDisabledPlayerRuneIds()
			.Select(static id => id.Entry)
			.ToHashSet(StringComparer.Ordinal);
	}

	public static IReadOnlySet<string> GetDefaultDisabledMonsterHexIds()
	{
		return new HashSet<string>(StringComparer.Ordinal);
	}

	public static IReadOnlySet<string> GetDefaultDisabledForgeIds()
	{
		return GetForgeIds(Version13DefaultDisabledForgeTypes);
	}

	public static void SaveDisabledPlayerRuneIds(IEnumerable<string> disabledIds)
	{
		EnsureLoaded();
		lock (SyncRoot)
		{
			_config.ConfigVersion = CurrentConfigVersion;
			_config.DisabledPlayerRuneIds = NormalizeConfigDisabledIds(disabledIds);
			SaveConfig(_config);
		}
	}

	public static void SaveEnemyHexCountsByAct(IReadOnlyList<int> counts)
	{
		EnsureLoaded();
		lock (SyncRoot)
		{
			_config.ConfigVersion = CurrentConfigVersion;
			_config.EnemyHexCountsByAct = NormalizeEnemyHexCounts(counts);
			SaveConfig(_config);
		}
	}

	public static void SaveSnapshot(HextechRunConfigurationSnapshot snapshot)
	{
		EnsureLoaded();
		lock (SyncRoot)
		{
			HextechRunConfigurationSnapshot normalized = NormalizeSnapshot(snapshot);
			_config.ConfigVersion = CurrentConfigVersion;
			_config.PlayerHexCountsByAct = normalized.PlayerHexCountsByAct;
			_config.EnemyHexCountsByAct = normalized.EnemyHexCountsByAct;
			_config.PlayerRuneRerollLimit = normalized.PlayerRuneRerollLimit;
			_config.MonsterHexRerollLimit = normalized.MonsterHexRerollLimit;
			_config.DisabledPlayerRuneIds = normalized.DisabledPlayerRuneIds;
			_config.DisabledMonsterHexIds = normalized.DisabledMonsterHexIds;
			_config.DisabledForgeIds = normalized.DisabledForgeIds;
			_config.FirstActRuneRarityWeights = FromRarityWeights(normalized.FirstActRuneRarityWeights);
			_config.NormalRuneRarityWeights = FromRarityWeights(normalized.NormalRuneRarityWeights);
			_config.SecondActAfterSilverRuneRarityWeights = FromRarityWeights(normalized.SecondActAfterSilverRuneRarityWeights);
			_config.ForgeRarityWeights = FromForgeRarityWeights(normalized.ForgeRarityWeights);
			_config.RandomForgeShopPrice = normalized.RandomForgeShopPrice;
			_config.RandomForgeDirectGrant = normalized.RandomForgeDirectGrant;
			_config.ModEnabled = normalized.ModEnabled;
			SaveConfig(_config);
		}
	}

	// 模组总开关的当前(实时)配置值。运行中应优先读「本局冻结快照」,仅在无 run 场景(菜单外/商店初始化兜底)用它。
	public static bool GetModEnabled()
	{
		EnsureLoaded();
		lock (SyncRoot)
		{
			return _config.ModEnabled;
		}
	}

	public static bool GetDefaultModEnabled()
	{
		return DefaultModEnabled;
	}

	private static void EnsureLoaded()
	{
		lock (SyncRoot)
		{
			if (_loaded)
			{
				return;
			}

			_config = LoadOrCreateConfig();
			_loaded = true;
		}
	}

	private static RuneConfig LoadOrCreateConfig()
	{
		string configPath = GetConfigPath();
		Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
		if (!File.Exists(configPath))
		{
			RuneConfig defaultConfig = CreateDefaultConfig();
			SaveConfig(defaultConfig);
			return defaultConfig;
		}

		try
		{
			RuneConfig? parsed = JsonSerializer.Deserialize<RuneConfig>(File.ReadAllText(configPath), JsonOptions);
			RuneConfig config = NormalizeLoadedConfig(parsed ?? new RuneConfig());
			SaveConfig(config);
			return config;
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][RuneConfig] Config read failed; using defaults: {ex.Message}", 2);
			RuneConfig config = CreateDefaultConfig();
			SaveConfig(config);
			return config;
		}
	}

	private static RuneConfig CreateDefaultConfig()
	{
		return new RuneConfig
		{
			ConfigVersion = CurrentConfigVersion,
			DisabledPlayerRuneIds = GetDefaultDisabledPlayerRuneIds().ToHashSet(StringComparer.Ordinal),
			PlayerHexCountsByAct = NormalizePlayerHexCounts(null),
			EnemyHexCountsByAct = NormalizeEnemyHexCounts(null),
			PlayerRuneRerollLimit = DefaultPlayerRuneRerollLimit,
			MonsterHexRerollLimit = DefaultMonsterHexRerollLimit,
			DisabledMonsterHexIds = GetDefaultDisabledMonsterHexIds().ToHashSet(StringComparer.Ordinal),
			DisabledForgeIds = GetDefaultDisabledForgeIds().ToHashSet(StringComparer.Ordinal),
			FirstActRuneRarityWeights = FromRarityWeights(DefaultFirstActRuneRarityWeights),
			NormalRuneRarityWeights = FromRarityWeights(DefaultNormalRuneRarityWeights),
			SecondActAfterSilverRuneRarityWeights = FromRarityWeights(DefaultSecondActAfterSilverRuneRarityWeights),
			ForgeRarityWeights = FromForgeRarityWeights(DefaultForgeRarityWeights),
			RandomForgeShopPrice = DefaultRandomForgeShopPrice,
			RandomForgeDirectGrant = DefaultRandomForgeDirectGrant,
			ModEnabled = DefaultModEnabled
		};
	}

	private static RuneConfig NormalizeLoadedConfig(RuneConfig config)
	{
		int previousConfigVersion = config.ConfigVersion;
		HashSet<string> disabledIds = NormalizeConfigDisabledIds(config.DisabledPlayerRuneIds);
		bool shouldMigrateLegacyEnemyHexDefault =
			previousConfigVersion < CurrentConfigVersion
			&& IsEnemyHexCountsEqual(config.EnemyHexCountsByAct, LegacyEnemyHexCountsDefault);
		bool shouldMigrateVersion9EnemyHexDefault =
			previousConfigVersion < 10
			&& IsEnemyHexCountsEqual(config.EnemyHexCountsByAct, Version9EnemyHexCountsDefault);
		if (previousConfigVersion < 4)
		{
			disabledIds.UnionWith(GetDefaultDisabledPlayerRuneIds());
		}
		else if (previousConfigVersion < 5)
		{
			disabledIds.UnionWith(GetPlayerRuneIds(Version5DefaultDisabledRuneTypes));
		}
		if (previousConfigVersion < 6)
		{
			disabledIds.UnionWith(GetPlayerRuneIds(Version6DefaultDisabledRuneTypes));
		}
		if (previousConfigVersion < 7)
		{
			disabledIds.UnionWith(GetPlayerRuneIds(Version7DefaultDisabledRuneTypes));
		}
		if (previousConfigVersion < 8)
		{
			disabledIds.ExceptWith(GetPlayerRuneIds(Version8DefaultEnabledRuneTypes));
			disabledIds.UnionWith(GetPlayerRuneIds(Version8DefaultDisabledRuneTypes));
		}
		if (previousConfigVersion < 11)
		{
			disabledIds.UnionWith(GetPlayerRuneIds(Version11DefaultDisabledRuneTypes));
		}
		if (previousConfigVersion < 13)
		{
			disabledIds.UnionWith(GetPlayerRuneIds(Version13DefaultDisabledRuneTypes));
		}

		config.ConfigVersion = CurrentConfigVersion;
		config.DisabledPlayerRuneIds = disabledIds;
		config.PlayerHexCountsByAct = NormalizePlayerHexCounts(config.PlayerHexCountsByAct);
		config.EnemyHexCountsByAct = shouldMigrateLegacyEnemyHexDefault || shouldMigrateVersion9EnemyHexDefault
			? NormalizeEnemyHexCounts(null)
			: NormalizeEnemyHexCounts(config.EnemyHexCountsByAct);
		config.PlayerRuneRerollLimit = ClampRerollLimit(previousConfigVersion < 12 ? DefaultPlayerRuneRerollLimit : config.PlayerRuneRerollLimit);
		config.MonsterHexRerollLimit = ClampRerollLimit(previousConfigVersion < 12 ? DefaultMonsterHexRerollLimit : config.MonsterHexRerollLimit);
		config.DisabledMonsterHexIds = NormalizeDisabledMonsterHexIds(config.DisabledMonsterHexIds);
		HashSet<string> disabledForgeIds = NormalizeDisabledForgeIds(config.DisabledForgeIds);
		if (previousConfigVersion < 13)
		{
			disabledForgeIds.UnionWith(GetForgeIds(Version13DefaultDisabledForgeTypes));
		}
		config.DisabledForgeIds = disabledForgeIds;
		HextechRarityWeights loadedFirstActWeights = ToRarityWeights(config.FirstActRuneRarityWeights, DefaultFirstActRuneRarityWeights);
		bool shouldMigrateLegacyFirstActWeights =
			previousConfigVersion < 14
			&& loadedFirstActWeights.Silver == Version14LegacyFirstActRuneRarityWeights.Silver
			&& loadedFirstActWeights.Gold == Version14LegacyFirstActRuneRarityWeights.Gold
			&& loadedFirstActWeights.Prismatic == Version14LegacyFirstActRuneRarityWeights.Prismatic;
		config.FirstActRuneRarityWeights = FromRarityWeights(shouldMigrateLegacyFirstActWeights
			? DefaultFirstActRuneRarityWeights
			: NormalizeRarityWeights(loadedFirstActWeights, DefaultFirstActRuneRarityWeights));
		config.NormalRuneRarityWeights = FromRarityWeights(NormalizeRarityWeights(
			ToRarityWeights(config.NormalRuneRarityWeights, DefaultNormalRuneRarityWeights),
			DefaultNormalRuneRarityWeights));
		config.SecondActAfterSilverRuneRarityWeights = FromRarityWeights(NormalizeRarityWeights(
			ToRarityWeights(config.SecondActAfterSilverRuneRarityWeights, DefaultSecondActAfterSilverRuneRarityWeights),
			DefaultSecondActAfterSilverRuneRarityWeights));
		config.ForgeRarityWeights = FromForgeRarityWeights(NormalizeForgeRarityWeights(
			ToForgeRarityWeights(config.ForgeRarityWeights, DefaultForgeRarityWeights),
			DefaultForgeRarityWeights));
		config.RandomForgeShopPrice = ClampRandomForgeShopPrice(config.RandomForgeShopPrice);
		return config;
	}

	public static int[] GetDefaultPlayerHexCountsByAct()
	{
		return NormalizePlayerHexCounts(null);
	}

	public static int[] GetDefaultEnemyHexCountsByAct()
	{
		return NormalizeEnemyHexCounts(null);
	}

	public static int ClampEnemyHexCount(int count)
	{
		return ClampActHexCount(count);
	}

	public static int ClampPlayerHexCount(int count)
	{
		return ClampActHexCount(count);
	}

	public static int ClampActHexCount(int count)
	{
		return Math.Clamp(count, MinActHexCount, MaxActHexCount);
	}

	public static int ClampRerollLimit(int limit)
	{
		return limit == InfiniteRerollLimit
			? InfiniteRerollLimit
			: Math.Clamp(limit, MinFiniteRerollLimit, MaxFiniteRerollLimit);
	}

	public static int StepRerollLimit(int current, int delta)
	{
		current = ClampRerollLimit(current);
		if (delta > 0)
		{
			return current == InfiniteRerollLimit || current >= MaxFiniteRerollLimit
				? InfiniteRerollLimit
				: current + 1;
		}

		if (delta < 0)
		{
			return current == InfiniteRerollLimit
				? MaxFiniteRerollLimit
				: Math.Max(MinFiniteRerollLimit, current - 1);
		}

		return current;
	}

	public static int GetDefaultPlayerRuneRerollLimit()
	{
		return DefaultPlayerRuneRerollLimit;
	}

	public static int GetDefaultMonsterHexRerollLimit()
	{
		return DefaultMonsterHexRerollLimit;
	}

	public static int ClampRarityWeight(int weight)
	{
		return Math.Clamp(weight, MinRarityWeight, MaxRarityWeight);
	}

	public static int ClampRandomForgeShopPrice(int price)
	{
		return Math.Clamp(price, MinRandomForgeShopPrice, MaxRandomForgeShopPrice);
	}

	public static HextechRarityWeights GetDefaultFirstActRuneRarityWeights()
	{
		return DefaultFirstActRuneRarityWeights;
	}

	public static HextechRarityWeights GetDefaultNormalRuneRarityWeights()
	{
		return DefaultNormalRuneRarityWeights;
	}

	public static HextechRarityWeights GetDefaultSecondActAfterSilverRuneRarityWeights()
	{
		return DefaultSecondActAfterSilverRuneRarityWeights;
	}

	public static HextechForgeRarityWeights GetDefaultForgeRarityWeights()
	{
		return DefaultForgeRarityWeights;
	}

	public static int GetDefaultRandomForgeShopPrice()
	{
		return DefaultRandomForgeShopPrice;
	}

	internal static HextechRunConfigurationSnapshot GetDefaultSnapshot()
	{
		return NormalizeSnapshot(new HextechRunConfigurationSnapshot(
			DefaultPlayerHexCountsByAct,
			DefaultEnemyHexCountsByAct,
			DefaultPlayerRuneRerollLimit,
			DefaultMonsterHexRerollLimit,
			GetDefaultDisabledPlayerRuneIds().ToHashSet(StringComparer.Ordinal),
			GetDefaultDisabledMonsterHexIds().ToHashSet(StringComparer.Ordinal),
			GetDefaultDisabledForgeIds().ToHashSet(StringComparer.Ordinal),
			DefaultFirstActRuneRarityWeights,
			DefaultNormalRuneRarityWeights,
			DefaultSecondActAfterSilverRuneRarityWeights,
			DefaultForgeRarityWeights,
			DefaultRandomForgeShopPrice,
			DefaultRandomForgeDirectGrant,
			DefaultModEnabled));
	}

	internal static HextechRunConfigurationSnapshot NormalizeSnapshot(HextechRunConfigurationSnapshot snapshot)
	{
		return new HextechRunConfigurationSnapshot(
			NormalizePlayerHexCounts(snapshot.PlayerHexCountsByAct),
			NormalizeEnemyHexCounts(snapshot.EnemyHexCountsByAct),
			ClampRerollLimit(snapshot.PlayerRuneRerollLimit),
			ClampRerollLimit(snapshot.MonsterHexRerollLimit),
			NormalizeDisabledPlayerRuneIds(snapshot.DisabledPlayerRuneIds),
			NormalizeDisabledMonsterHexIds(snapshot.DisabledMonsterHexIds),
			NormalizeDisabledForgeIds(snapshot.DisabledForgeIds),
			NormalizeRarityWeights(snapshot.FirstActRuneRarityWeights, DefaultFirstActRuneRarityWeights),
			NormalizeRarityWeights(snapshot.NormalRuneRarityWeights, DefaultNormalRuneRarityWeights),
			NormalizeRarityWeights(snapshot.SecondActAfterSilverRuneRarityWeights, DefaultSecondActAfterSilverRuneRarityWeights),
			NormalizeForgeRarityWeights(snapshot.ForgeRarityWeights, DefaultForgeRarityWeights),
			ClampRandomForgeShopPrice(snapshot.RandomForgeShopPrice),
			snapshot.RandomForgeDirectGrant,
			snapshot.ModEnabled);
	}

	internal static HextechRarityWeights NormalizeRarityWeights(HextechRarityWeights weights, HextechRarityWeights fallback)
	{
		HextechRarityWeights normalized = new(
			ClampRarityWeight(weights.Silver),
			ClampRarityWeight(weights.Gold),
			ClampRarityWeight(weights.Prismatic));
		return normalized.Total > 0 ? normalized : fallback;
	}

	internal static HextechForgeRarityWeights NormalizeForgeRarityWeights(HextechForgeRarityWeights weights, HextechForgeRarityWeights fallback)
	{
		HextechForgeRarityWeights normalized = new(
			ClampRarityWeight(weights.Silver),
			ClampRarityWeight(weights.Gold),
			ClampRarityWeight(weights.Prismatic));
		return normalized.Total > 0 ? normalized : fallback;
	}

	internal static int[] NormalizePlayerHexCounts(IReadOnlyList<int>? counts)
	{
		return NormalizeActHexCounts(counts, DefaultPlayerHexCountsByAct);
	}

	private static int[] NormalizeEnemyHexCounts(IReadOnlyList<int>? counts)
	{
		return NormalizeActHexCounts(counts, DefaultEnemyHexCountsByAct);
	}

	private static int[] NormalizeActHexCounts(IReadOnlyList<int>? counts, IReadOnlyList<int> defaults)
	{
		int[] normalized = defaults.ToArray();
		if (counts == null)
		{
			return normalized;
		}

		for (int i = 0; i < Math.Min(HexActCount, counts.Count); i++)
		{
			normalized[i] = ClampActHexCount(counts[i]);
		}

		return normalized;
	}

	private static bool IsEnemyHexCountsEqual(IReadOnlyList<int>? counts, IReadOnlyList<int> expected)
	{
		if (counts == null || counts.Count < expected.Count)
		{
			return false;
		}

		for (int i = 0; i < expected.Count; i++)
		{
			if (counts[i] != expected[i])
			{
				return false;
			}
		}

		return true;
	}

	private static HashSet<string> NormalizeConfigDisabledIds(IEnumerable<string>? ids)
	{
		return HextechPlayerRuneConfigIds.Normalize(ids);
	}

	private static HashSet<string> NormalizeStringIds(IEnumerable<string>? ids, IReadOnlySet<string> validIds)
	{
		return (ids ?? [])
			.Where(static id => !string.IsNullOrWhiteSpace(id))
			.Select(static id => id.Trim())
			.Distinct(StringComparer.Ordinal)
			.Where(validIds.Contains)
			.OrderBy(static id => id, StringComparer.Ordinal)
			.ToHashSet(StringComparer.Ordinal);
	}

	private static HashSet<string> NormalizeConfigStringIds(IEnumerable<string>? ids)
	{
		return (ids ?? [])
			.Where(static id => !string.IsNullOrWhiteSpace(id))
			.Select(static id => id.Trim())
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static id => id, StringComparer.Ordinal)
			.ToHashSet(StringComparer.Ordinal);
	}

	private static HashSet<string> GetPlayerRuneIds(IEnumerable<Type> runeTypes)
	{
		return HextechPlayerRuneConfigIds.FromTypes(runeTypes);
	}

	private static HashSet<string> GetForgeIds(IEnumerable<Type> forgeTypes)
	{
		return forgeTypes
			.Select(static type => ModelDb.GetId(type).Entry)
			.ToHashSet(StringComparer.Ordinal);
	}

	private static HextechRarityWeights ToRarityWeights(RarityWeightConfig? config, HextechRarityWeights fallback)
	{
		return config == null
			? fallback
			: new HextechRarityWeights(config.Silver, config.Gold, config.Prismatic);
	}

	private static HextechForgeRarityWeights ToForgeRarityWeights(RarityWeightConfig? config, HextechForgeRarityWeights fallback)
	{
		return config == null
			? fallback
			: new HextechForgeRarityWeights(config.Silver, config.Gold, config.Prismatic);
	}

	private static RarityWeightConfig FromRarityWeights(HextechRarityWeights weights)
	{
		return new RarityWeightConfig
		{
			Silver = weights.Silver,
			Gold = weights.Gold,
			Prismatic = weights.Prismatic
		};
	}

	private static RarityWeightConfig FromForgeRarityWeights(HextechForgeRarityWeights weights)
	{
		return new RarityWeightConfig
		{
			Silver = weights.Silver,
			Gold = weights.Gold,
			Prismatic = weights.Prismatic
		};
	}

	private static void SaveConfig(RuneConfig config)
	{
		string configPath = GetConfigPath();
		Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
		File.WriteAllText(configPath, JsonSerializer.Serialize(config, JsonOptions));
	}

	private static string GetConfigPath()
	{
		return Path.Combine(GetDataDirectory(), ConfigFileName);
	}

	private static string GetDataDirectory()
	{
		try
		{
			string godotUserDir = OS.GetUserDataDir();
			if (!string.IsNullOrWhiteSpace(godotUserDir))
			{
				return Path.Combine(godotUserDir, ModInfo.Id);
			}
		}
		catch
		{
			// Fall back to a normal per-user directory when Godot paths are unavailable.
		}

		string baseDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
		if (string.IsNullOrWhiteSpace(baseDir))
		{
			baseDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
		}

		return Path.Combine(baseDir, "SlayTheSpire2", ModInfo.Id);
	}

	private sealed class RuneConfig
	{
		[JsonPropertyName("config_version")]
		public int ConfigVersion { get; set; }

		[JsonPropertyName("disabled_player_rune_ids")]
		public HashSet<string> DisabledPlayerRuneIds { get; set; } = new(StringComparer.Ordinal);

		[JsonPropertyName("player_hex_counts_by_act")]
		public int[]? PlayerHexCountsByAct { get; set; }

		[JsonPropertyName("enemy_hex_counts_by_act")]
		public int[]? EnemyHexCountsByAct { get; set; }

		[JsonPropertyName("player_rune_reroll_limit")]
		public int PlayerRuneRerollLimit { get; set; } = DefaultPlayerRuneRerollLimit;

		[JsonPropertyName("monster_hex_reroll_limit")]
		public int MonsterHexRerollLimit { get; set; } = DefaultMonsterHexRerollLimit;

		[JsonPropertyName("disabled_monster_hex_ids")]
		public HashSet<string> DisabledMonsterHexIds { get; set; } = new(StringComparer.Ordinal);

		[JsonPropertyName("disabled_forge_ids")]
		public HashSet<string> DisabledForgeIds { get; set; } = new(StringComparer.Ordinal);

		[JsonPropertyName("first_act_rune_rarity_weights")]
		public RarityWeightConfig? FirstActRuneRarityWeights { get; set; }

		[JsonPropertyName("normal_rune_rarity_weights")]
		public RarityWeightConfig? NormalRuneRarityWeights { get; set; }

		[JsonPropertyName("second_act_after_silver_rune_rarity_weights")]
		public RarityWeightConfig? SecondActAfterSilverRuneRarityWeights { get; set; }

		[JsonPropertyName("forge_rarity_weights")]
		public RarityWeightConfig? ForgeRarityWeights { get; set; }

		[JsonPropertyName("random_forge_shop_price")]
		public int RandomForgeShopPrice { get; set; } = DefaultRandomForgeShopPrice;

		[JsonPropertyName("random_forge_direct_grant")]
		public bool RandomForgeDirectGrant { get; set; } = DefaultRandomForgeDirectGrant;

		[JsonPropertyName("mod_enabled")]
		public bool ModEnabled { get; set; } = DefaultModEnabled;
	}

	private sealed class RarityWeightConfig
	{
		[JsonPropertyName("silver")]
		public int Silver { get; set; }

		[JsonPropertyName("gold")]
		public int Gold { get; set; }

		[JsonPropertyName("prismatic")]
		public int Prismatic { get; set; }
	}
}
