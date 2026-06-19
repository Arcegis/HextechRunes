using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static class HextechRuneConfiguration
{
	private const string ConfigFileName = "rune_config.json";
	private const int CurrentConfigVersion = 5;
	private const int EnemyHexActCount = 3;
	private const int MinEnemyHexCount = 0;
	private const int MaxEnemyHexCount = 6;
	private static readonly int[] DefaultEnemyHexCountsByAct = [ 1, 1, 1 ];
	private static readonly int[] LegacyEnemyHexCountsDefault = [ 1, 2, 3 ];
	private static readonly Type[] Version5DefaultDisabledRuneTypes =
	[
		typeof(DemonFormUpgradeRune),
		typeof(TyrannyUpgradeRune)
	];

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

	internal static HashSet<string> NormalizeDisabledPlayerRuneIds(IEnumerable<string>? ids)
	{
		EnsureLoaded();
		lock (SyncRoot)
		{
			return NormalizeConfigDisabledIds(ids);
		}
	}

	public static IReadOnlySet<string> GetDefaultDisabledPlayerRuneIds()
	{
		return HextechCatalog.GetDefaultDisabledPlayerRuneIds()
			.Select(static id => id.Entry)
			.ToHashSet(StringComparer.Ordinal);
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
			EnemyHexCountsByAct = NormalizeEnemyHexCounts(null)
		};
	}

	private static RuneConfig NormalizeLoadedConfig(RuneConfig config)
	{
		int previousConfigVersion = config.ConfigVersion;
		HashSet<string> disabledIds = NormalizeConfigDisabledIds(config.DisabledPlayerRuneIds);
		bool shouldMigrateLegacyEnemyHexDefault =
			previousConfigVersion < CurrentConfigVersion
			&& IsEnemyHexCountsEqual(config.EnemyHexCountsByAct, LegacyEnemyHexCountsDefault);
		if (previousConfigVersion < 4)
		{
			disabledIds.UnionWith(GetDefaultDisabledPlayerRuneIds());
		}
		else if (previousConfigVersion < 5)
		{
			disabledIds.UnionWith(GetPlayerRuneIds(Version5DefaultDisabledRuneTypes));
		}

		config.ConfigVersion = CurrentConfigVersion;
		config.DisabledPlayerRuneIds = disabledIds;
		config.EnemyHexCountsByAct = shouldMigrateLegacyEnemyHexDefault
			? NormalizeEnemyHexCounts(null)
			: NormalizeEnemyHexCounts(config.EnemyHexCountsByAct);
		return config;
	}

	public static int[] GetDefaultEnemyHexCountsByAct()
	{
		return NormalizeEnemyHexCounts(null);
	}

	public static int ClampEnemyHexCount(int count)
	{
		return Math.Clamp(count, MinEnemyHexCount, MaxEnemyHexCount);
	}

	private static int[] NormalizeEnemyHexCounts(IReadOnlyList<int>? counts)
	{
		int[] normalized = DefaultEnemyHexCountsByAct.ToArray();
		if (counts == null)
		{
			return normalized;
		}

		for (int i = 0; i < Math.Min(EnemyHexActCount, counts.Count); i++)
		{
			normalized[i] = ClampEnemyHexCount(counts[i]);
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

	private static HashSet<string> GetPlayerRuneIds(IEnumerable<Type> runeTypes)
	{
		return HextechPlayerRuneConfigIds.FromTypes(runeTypes);
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

		[JsonPropertyName("enemy_hex_counts_by_act")]
		public int[]? EnemyHexCountsByAct { get; set; }
	}
}
