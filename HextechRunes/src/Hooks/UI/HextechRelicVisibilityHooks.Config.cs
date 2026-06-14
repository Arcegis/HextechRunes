using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace HextechRunes;

internal static partial class HextechRelicVisibilityHooks
{
	private static ModUiConfig LoadOrCreateConfig()
	{
		string configPath = GetConfigPath();
		Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
		if (!File.Exists(configPath))
		{
			ModUiConfig defaultConfig = new();
			SaveConfig(defaultConfig);
			return defaultConfig;
		}

		try
		{
			ModUiConfig? parsed = JsonSerializer.Deserialize<ModUiConfig>(File.ReadAllText(configPath), JsonOptions);
			ModUiConfig config = parsed ?? new ModUiConfig();
			SaveConfig(config);
			return config;
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Relic visibility config read failed; using defaults: {ex.Message}", 2);
			ModUiConfig config = new();
			SaveConfig(config);
			return config;
		}
	}

	private static void SaveConfig(ModUiConfig config)
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

	private sealed class ModUiConfig
	{
		[JsonPropertyName("hide_relics")]
		public bool HideRelics { get; set; }
	}
}
