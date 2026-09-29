#if !STS2_109_OR_NEWER
namespace HextechRunes;

/// <summary>跑局开始/读档时的 SavedProperty 载体自检入口（版本差异见 .Official.cs）。</summary>
internal static class HextechSavedPropertyAuditCompat
{
	// 0.107.1 的自检在 net-id 规范化后缀（HextechSavedPropertyNetIdHooks）里完成，跑局开始时无事可做。
	internal static void RunAuditOnRunStartOnce()
	{
	}
}
#endif
