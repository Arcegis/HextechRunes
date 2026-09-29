namespace HextechRunes;

/// <summary>
/// 可由 <c>HEXTECH_VERBOSE_LOG=1</c> 或 <c>true</c> 开启的诊断 Info 日志，默认关闭。
/// Warn/Error 与加载确认仍直接输出。此包装接收已构造的字符串；需要避免格式化开销时，
/// 调用方应先检查 <see cref="Verbose"/>。
/// </summary>
internal static class HextechLog
{
	private static bool _verbose = ReadVerboseFlagFromEnvironment();

	internal static bool Verbose
	{
		get => _verbose;
		set => _verbose = value;
	}

	internal static void Info(string text)
	{
		if (_verbose)
		{
			// skipFrames=2：跳过本包装方法，让日志归因到真正的调用点。
			Log.Info(text, 2);
		}
	}

	private static bool ReadVerboseFlagFromEnvironment()
	{
		try
		{
			string? value = Environment.GetEnvironmentVariable("HEXTECH_VERBOSE_LOG");
			return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}
}
