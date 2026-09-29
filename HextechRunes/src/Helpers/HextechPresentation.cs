namespace HextechRunes;

/// <summary>
/// 表现层调用（特效、闪光、费用/计数刷新、节点缩放）的统一兜底：失败只记警告，不打断已经确定的共享状态
/// （设计哲学第 4 节「表现层不能先于同步写入抛异常」）。只包纯表现调用，不要用来包命令链或状态写入。
/// </summary>
internal static class HextechPresentation
{
	internal static void TryRun(string tag, string failureMessage, Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			HextechLog.Warn(tag, $"{failureMessage}: {ex.Message}");
		}
	}
}
