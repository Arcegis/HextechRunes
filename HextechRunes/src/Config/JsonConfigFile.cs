using System.Text.Json;

namespace HextechRunes;

/// <summary>
/// 模组数据目录下的单个 JSON 配置文件(rune_config.json、ui_config.json)的读写口径:
/// 文件不存在就写入默认值;JSON 损坏时先备份为 <c>.corrupt.bak</c>,备份成功才用默认值覆盖;
/// 其余读取失败(权限、I/O 等)只在内存里用默认值,不覆盖文件。写盘先写临时文件再替换,
/// 写到一半崩溃或断电不会留下截断的 JSON(否则下次载入会被当作损坏配置回落默认)。
/// </summary>
internal static class JsonConfigFile
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true
	};

	/// <param name="migrate">
	/// 解析结果(文件内容是 <c>null</c> 时为新实例)→ 规范化后的配置,以及是否把它写回文件。
	/// </param>
	internal static T Load<T>(
		string fileName,
		string logTag,
		Func<T> createDefault,
		Func<T, (T Config, bool Rewrite)> migrate)
		where T : class, new()
	{
		string? path = null;
		try
		{
			path = HextechDataPaths.GetFilePath(fileName);
			if (!File.Exists(path))
			{
				T defaults = createDefault();
				Save(fileName, logTag, defaults);
				return defaults;
			}

			T parsed = JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ?? new T();
			(T config, bool rewrite) = migrate(parsed);
			if (rewrite)
			{
				Save(fileName, logTag, config);
			}

			return config;
		}
		catch (JsonException ex)
		{
			HextechLog.Warn(logTag, $"{fileName} JSON is invalid; using defaults: {ex.Message}");
			T config = createDefault();
			if (path != null && TryBackupCorruptFile(path, logTag))
			{
				Save(fileName, logTag, config);
			}

			return config;
		}
		catch (Exception ex)
		{
			HextechLog.Warn(logTag, $"{fileName} read failed; using in-memory defaults without overwriting the file: {ex}");
			return createDefault();
		}
	}

	internal static void Save<T>(string fileName, string logTag, T config)
	{
		try
		{
			string path = HextechDataPaths.GetFilePath(fileName);
			Directory.CreateDirectory(HextechDataPaths.GetDataDirectory());
			string tempPath = path + ".tmp";
			File.WriteAllText(tempPath, JsonSerializer.Serialize(config, JsonOptions));
			File.Move(tempPath, path, overwrite: true);
		}
		catch (Exception ex)
		{
			HextechLog.Warn(logTag, $"{fileName} write failed: {ex.Message}");
		}
	}

	private static bool TryBackupCorruptFile(string path, string logTag)
	{
		try
		{
			File.Copy(path, path + ".corrupt.bak", overwrite: true);
			return true;
		}
		catch (Exception ex)
		{
			HextechLog.Warn(logTag, $"Could not back up corrupt {Path.GetFileName(path)}; original file will not be overwritten: {ex.Message}");
			return false;
		}
	}
}
