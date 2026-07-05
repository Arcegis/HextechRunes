using System.Text.Json;

namespace HextechRunes;

internal static partial class HextechTelemetry
{
	private static TelemetryConfig LoadConfig()
	{
		EnsureConfigFile();
		try
		{
			string json = File.ReadAllText(GetConfigPath());
			TelemetryConfig? config = JsonSerializer.Deserialize<TelemetryConfig>(json, JsonOptions);
			if (config != null && !string.IsNullOrWhiteSpace(config.Endpoint))
			{
				return config;
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Telemetry config read failed: {ex.Message}");
		}

		return new TelemetryConfig(true, DefaultEndpoint);
	}

	private static void EnsureConfigFile()
	{
		string configPath = GetConfigPath();
		if (File.Exists(configPath))
		{
			return;
		}

		Directory.CreateDirectory(GetDataDirectory());
		TelemetryConfig config = new(true, DefaultEndpoint);
		File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions(JsonOptions) { WriteIndented = true }));
	}

	private static string GetDataDirectory()
	{
		try
		{
			string godotUserDir = Godot.OS.GetUserDataDir();
			if (!string.IsNullOrWhiteSpace(godotUserDir))
			{
				return Path.Combine(godotUserDir, ModInfo.Id);
			}
		}
		catch
		{
			// Fall back to a normal per-user directory when Godot paths are unavailable.
		}

		string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		if (string.IsNullOrWhiteSpace(baseDir))
		{
			baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		}

		return Path.Combine(baseDir, "SlayTheSpire2", ModInfo.Id);
	}

	private static string GetConfigPath()
	{
		return Path.Combine(GetDataDirectory(), ConfigFileName);
	}

	private static string GetPendingPath()
	{
		return Path.Combine(GetDataDirectory(), PendingFileName);
	}
}
