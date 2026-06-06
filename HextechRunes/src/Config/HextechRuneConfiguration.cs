using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal static class HextechRuneConfiguration
{
	private const string ConfigFileName = "rune_config.json";
	private const int CurrentConfigVersion = 2;

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
			DisabledPlayerRuneIds = GetDefaultDisabledPlayerRuneIds().ToHashSet(StringComparer.Ordinal)
		};
	}

	private static RuneConfig NormalizeLoadedConfig(RuneConfig config)
	{
		HashSet<string> disabledIds = NormalizeConfigDisabledIds(config.DisabledPlayerRuneIds);
		if (config.ConfigVersion < CurrentConfigVersion)
		{
			disabledIds.UnionWith(GetDefaultDisabledPlayerRuneIds());
		}

		config.ConfigVersion = CurrentConfigVersion;
		config.DisabledPlayerRuneIds = disabledIds;
		return config;
	}

	private static HashSet<string> NormalizeConfigDisabledIds(IEnumerable<string>? ids)
	{
		HashSet<string> configurableIds = HextechCatalog.GetConfigurablePlayerRuneIds()
			.Select(static id => id.Entry)
			.ToHashSet(StringComparer.Ordinal);
		return NormalizeIds(ids)
			.Where(configurableIds.Contains)
			.ToHashSet(StringComparer.Ordinal);
	}

	private static HashSet<string> NormalizeIds(IEnumerable<string>? ids)
	{
		return (ids ?? [])
			.Where(static id => !string.IsNullOrWhiteSpace(id))
			.Select(static id => id.Trim())
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static id => id, StringComparer.Ordinal)
			.ToHashSet(StringComparer.Ordinal);
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
	}
}
