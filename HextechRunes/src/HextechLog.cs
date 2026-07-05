namespace HextechRunes;

/// <summary>
/// 诊断日志门控:模组冗长的 <c>Log.Info</c> 经此收敛到一个开关后面，默认静默以降噪
/// （事件级路径如 EnemyUi 刷新原本每次都打多行）。<c>Warn</c>/<c>Error</c> 仍直接走
/// <see cref="Log"/> 始终输出，模组加载确认行也保持始终输出。
///
/// 需要排错时设环境变量 <c>HEXTECH_VERBOSE_LOG=1</c>（或 <c>true</c>）即可恢复全部 Info；
/// 也可在运行时通过 <see cref="Verbose"/> 切换。门控在调用前判断，关闭时连日志字符串都不再产出。
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
