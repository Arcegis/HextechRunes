namespace HextechRunes;

/// <summary>
/// 战斗侧补丁。各目标的补丁类以嵌套类形式分布在同名分部文件里,由 <see cref="HextechPatcher"/> 统一应用;
/// 这里只保留尚未迁移的玩家符文 Hook 安装入口。
/// </summary>
internal static partial class HextechCombatHooks
{
	public static void Install(Harmony harmony)
	{
		HextechPlayerRuneHooks.Install(harmony);
	}
}
