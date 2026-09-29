using MegaCrit.Sts2.Core.Logging;

namespace HextechRunesSponsorPack;

/// <summary>
/// 拓展包统一带 <c>[HextechRunesSponsorPack][Tag]</c> 前缀的日志入口(本体的 HextechLog 是 internal,拓展包用不到)。
/// 与本体不同,Info 不受 HEXTECH_VERBOSE_LOG 门控:拓展包的 Info 只有启动摘要与少量低频事件,保持一直输出。
/// </summary>
internal static class SponsorLog
{
	// 原版 Log.* 的默认 skipFrames=2 指向直接调用方;经本包装多一层,所以传 3。
	private const int CallerSkipFrames = 3;

	internal static void Info(string tag, string message)
	{
		Log.Info(Format(tag, message), CallerSkipFrames);
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
}
