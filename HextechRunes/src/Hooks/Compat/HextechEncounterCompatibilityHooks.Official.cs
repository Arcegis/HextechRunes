#if !STS2_107_1
namespace HextechRunes;

// 0.108 起原版昆虫法师在缺私人蜂巢时已自行处理 SpitMove，本变体不安装任何补丁（0.107.1 见 .Legacy.cs）。
internal static class HextechEncounterCompatibilityHooks
{
	internal static bool ShouldRunOriginalEntomancerSpitMove(bool hasPersonalHive)
	{
		return true;
	}
}
#endif
