using System.Text.Json;
using System.Text.Json.Serialization;

namespace HextechRunes;

/// <summary>
/// 本机 UI 偏好:隐藏遗物开关、更新提示、敌方海克斯折叠、选择二次确认,以及隐藏遗物开关当前的勾选状态。
/// 只改变本机界面,不进运行配置快照,联机各端可以不同。持久化在 <c>ui_config.json</c>(路径与字段名保持兼容)。
/// </summary>
/// <remarks>
/// 首次读取时才加载文件(不在补丁安装阶段做 IO)。写盘只在值确实被修改时发生:
/// 配置菜单的「保存并关闭」经 <see cref="SaveMenuPreferences"/> 一次写入四项偏好。
/// </remarks>
internal static class HextechUiPreferences
{
	internal const bool DefaultShowHiddenRelicsToggle = false;
	internal const bool DefaultShowUpdateNotice = true;
	internal const bool DefaultCollapseEnemyHexes = false;
	internal const bool DefaultConfirmRuneSelection = false;

	private const string ConfigFileName = "ui_config.json";
	private const string LogTag = "Mayhem";
	private const int CurrentUiConfigVersion = 1;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true
	};

	private static ModUiConfig? _config;

	private static ModUiConfig Config => _config ??= LoadOrCreateConfig();

	/// <summary>战斗界面是否显示「隐藏遗物」开关。</summary>
	internal static bool ShowHiddenRelicsToggle => Config.ShowHiddenRelicsToggle;

	internal static bool ShowUpdateNotice => Config.ShowUpdateNotice;

	/// <summary>
	/// 折叠敌方海克斯(默认关):开=顶栏地图按钮左侧一个折叠按钮,点开在下方弹出敌方海克斯窗口;
	/// 关=敌方海克斯直接平铺在顶栏 modifiers 里。读取见 <see cref="HextechEnemyUi"/>。
	/// </summary>
	internal static bool CollapseEnemyHexes => Config.CollapseEnemyHexes;

	/// <summary>
	/// 海克斯选择二次确认(默认关):开=点卡片只标记待定,按"确认"才提交;关=点卡片立即选定。
	/// 只改变本机界面何时提交,提交内容与同步协议不变。
	/// </summary>
	internal static bool ConfirmRuneSelection => Config.ConfirmRuneSelection;

	/// <summary>「隐藏遗物」开关当前是否勾选(仅在开关显示时生效)。</summary>
	internal static bool HideRelics => Config.HideRelics;

	internal static void SetHideRelics(bool hideRelics)
	{
		Config.HideRelics = hideRelics;
		Save();
		HextechLog.Info(LogTag, $"hide_ui={hideRelics}.");
	}

	/// <summary>配置菜单「保存并关闭」:四项偏好一次写盘。</summary>
	internal static void SaveMenuPreferences(
		bool showHiddenRelicsToggle,
		bool showUpdateNotice,
		bool collapseEnemyHexes,
		bool confirmRuneSelection)
	{
		ModUiConfig config = Config;
		config.ShowHiddenRelicsToggle = showHiddenRelicsToggle;
		config.ShowUpdateNotice = showUpdateNotice;
		config.CollapseEnemyHexes = collapseEnemyHexes;
		config.ConfirmRuneSelection = confirmRuneSelection;
		Save();
		HextechLog.Info(
			LogTag,
			$"UI preferences saved: show_hidden_ui_toggle={showHiddenRelicsToggle} show_update_notice={showUpdateNotice} collapse_enemy_hexes={collapseEnemyHexes} confirm_rune_selection={confirmRuneSelection}.");
	}

	private static ModUiConfig LoadOrCreateConfig()
	{
		string? configPath = null;
		try
		{
			configPath = GetConfigPath();
			if (!File.Exists(configPath))
			{
				ModUiConfig defaultConfig = CreateCurrentUiConfig();
				Save(defaultConfig);
				return defaultConfig;
			}

			ModUiConfig? parsed = JsonSerializer.Deserialize<ModUiConfig>(File.ReadAllText(configPath), JsonOptions);
			ModUiConfig config = parsed ?? new ModUiConfig();
			// 0.8.4 一次性强制回默认(与 rune_config 的 v15 重置同批):旧 UI 偏好整体丢弃。
			if (config.ConfigVersion < CurrentUiConfigVersion)
			{
				HextechLog.Info(LogTag, $"UI config version {config.ConfigVersion} < {CurrentUiConfigVersion}; forcing reset to defaults (0.8.4).");
				config = CreateCurrentUiConfig();
			}

			Save(config);
			return config;
		}
		catch (JsonException ex)
		{
			HextechLog.Warn(LogTag, $"Relic visibility config JSON is invalid; using defaults: {ex.Message}");
			ModUiConfig config = CreateCurrentUiConfig();
			if (configPath != null && TryBackupCorruptConfig(configPath))
			{
				Save(config);
			}

			return config;
		}
		catch (UnauthorizedAccessException ex)
		{
			HextechLog.Warn(LogTag, $"Relic visibility config read was denied; using in-memory defaults without overwriting the file: {ex.Message}");
			return CreateCurrentUiConfig();
		}
		catch (IOException ex)
		{
			HextechLog.Warn(LogTag, $"Relic visibility config read failed due to I/O; using in-memory defaults without overwriting the file: {ex.Message}");
			return CreateCurrentUiConfig();
		}
		catch (Exception ex)
		{
			HextechLog.Error(LogTag, $"Unexpected relic visibility config read failure; using in-memory defaults without overwriting the file: {ex}");
			return CreateCurrentUiConfig();
		}
	}

	private static ModUiConfig CreateCurrentUiConfig()
	{
		return new ModUiConfig { ConfigVersion = CurrentUiConfigVersion };
	}

	private static bool TryBackupCorruptConfig(string configPath)
	{
		try
		{
			File.Copy(configPath, configPath + ".corrupt.bak", overwrite: true);
			return true;
		}
		catch (UnauthorizedAccessException ex)
		{
			HextechLog.Warn(LogTag, $"Could not back up corrupt relic visibility config; original file will not be overwritten: {ex.Message}");
			return false;
		}
		catch (IOException ex)
		{
			HextechLog.Warn(LogTag, $"Could not back up corrupt relic visibility config; original file will not be overwritten: {ex.Message}");
			return false;
		}
		catch (Exception ex)
		{
			HextechLog.Error(LogTag, $"Unexpected relic visibility config backup failure; original file will not be overwritten: {ex}");
			return false;
		}
	}

	private static void Save()
	{
		Save(Config);
	}

	private static void Save(ModUiConfig config)
	{
		try
		{
			string configPath = GetConfigPath();
			Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
			string serialized = JsonSerializer.Serialize(config, JsonOptions);
			File.WriteAllText(configPath, serialized);
		}
		catch (Exception ex)
		{
			HextechLog.Warn(LogTag, $"Relic visibility config write failed: {ex.Message}");
		}
	}

	private static string GetConfigPath()
	{
		return HextechDataPaths.GetFilePath(ConfigFileName);
	}

	private sealed class ModUiConfig
	{
		// 默认必须是 0:旧文件无此字段时反序列化保留属性初始值,0 才能触发一次性重置;
		// 新建/重置的实例由 CreateCurrentUiConfig 显式设为 CurrentUiConfigVersion。
		[JsonPropertyName("config_version")]
		public int ConfigVersion { get; set; }

		[JsonPropertyName("show_hidden_relics_toggle")]
		public bool ShowHiddenRelicsToggle { get; set; } = DefaultShowHiddenRelicsToggle;

		[JsonPropertyName("show_update_notice")]
		public bool ShowUpdateNotice { get; set; } = DefaultShowUpdateNotice;

		[JsonPropertyName("collapse_enemy_hexes")]
		public bool CollapseEnemyHexes { get; set; } = DefaultCollapseEnemyHexes;

		[JsonPropertyName("confirm_rune_selection")]
		public bool ConfirmRuneSelection { get; set; } = DefaultConfirmRuneSelection;

		[JsonPropertyName("hide_relics")]
		public bool HideRelics { get; set; }
	}
}
