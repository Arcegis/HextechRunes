using System.Text.Json;

namespace HextechRunes;

internal static partial class HextechTelemetry
{
	private static TelemetryConfig LoadConfig()
	{
		EnsureConfigFile();
		try
		{
			return ParseConfig(File.ReadAllText(GetConfigPath()));
		}
		catch (Exception ex)
		{
			HextechLog.Warn("Mayhem", $"Telemetry config read failed: {ex.Message}");
		}

		return new TelemetryConfig(true, DefaultEndpoint);
	}

	/// <summary>
	/// 解析 telemetry_config.json。endpoint 缺失/为空时只补默认地址,保留用户写的 enabled
	/// (<c>{"enabled":false}</c> 必须仍是关闭);JSON 损坏时由调用方回落默认,行为保持不变。
	/// </summary>
	private static TelemetryConfig ParseConfig(string json)
	{
		TelemetryConfig? config = JsonSerializer.Deserialize<TelemetryConfig>(json, JsonOptions);
		if (config == null)
		{
			return new TelemetryConfig(true, DefaultEndpoint);
		}

		return string.IsNullOrWhiteSpace(config.Endpoint)
			? config with { Endpoint = DefaultEndpoint }
			: config;
	}

	internal static (bool Enabled, string Endpoint) ParseConfigForTests(string json)
	{
		TelemetryConfig config = ParseConfig(json);
		return (config.Enabled, config.Endpoint);
	}

	internal static string DefaultEndpointForTests => DefaultEndpoint;

	private static void EnsureConfigFile()
	{
		string configPath = GetConfigPath();
		if (File.Exists(configPath))
		{
			return;
		}

		Directory.CreateDirectory(HextechDataPaths.GetDataDirectory());
		TelemetryConfig config = new(true, DefaultEndpoint);
		File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions(JsonOptions) { WriteIndented = true }));
	}

	private static string GetConfigPath()
	{
		return HextechDataPaths.GetFilePath(ConfigFileName);
	}

	private static string GetPendingPath()
	{
		return HextechDataPaths.GetFilePath(PendingFileName);
	}
}
