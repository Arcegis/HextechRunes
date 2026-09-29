namespace HextechRunes;

/// <summary>
/// 统一带 <c>[HextechRunes][Tag]</c> 前缀的日志入口。Info 为诊断日志，默认关闭，可由
/// <c>HEXTECH_VERBOSE_LOG=1</c> 或 <c>true</c> 开启；Warn/Error 始终输出。消息由调用方构造好传入；
/// 需要避免 Info 的格式化开销时，调用方应先检查 <see cref="Verbose"/>。
/// </summary>
internal static class HextechLog
{
	// 原版 Log.* 的默认 skipFrames=2 指向直接调用方；经本包装多一层，所以传 3，
	// 让 Error 的堆栈从真正的调用点开始。
	private const int CallerSkipFrames = 3;

	internal static bool Verbose { get; } = ReadVerboseFlagFromEnvironment();

	internal static void Info(string tag, string message)
	{
		if (Verbose)
		{
			Log.Info(Format(tag, message), CallerSkipFrames);
		}
	}

	internal static void Warn(string tag, string message)
	{
		Log.Warn(Format(tag, message), CallerSkipFrames);
	}

	internal static void Error(string tag, string message)
	{
		Log.Error(Format(tag, message), CallerSkipFrames);
	}

	internal static string Format(string tag, string message)
	{
		return $"[{ModInfo.Id}][{tag}] {message}";
	}

	private static bool ReadVerboseFlagFromEnvironment()
	{
		string? value = Environment.GetEnvironmentVariable("HEXTECH_VERBOSE_LOG");
		return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
	}
}
