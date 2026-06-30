using MegaCrit.Sts2.Core.Entities.Players;

namespace HextechRunes;

// 梦魇(仅鸡煲) —— 黑暗充能球(DarkOrb)触发被动时,对生命值最低的敌人造成等同于该球当前计数(EvokeVal)的伤害。
// 真正的伤害在 HextechNightmareHooks(Harmony 改 DarkOrb.BeforeTurnEndOrbTrigger)。本类仅负责门控。
public sealed class NightmareRune : HextechRelicBase
{
	public override bool IsAvailableForPlayer(Player player) => IsDefectPlayer(player);
}
