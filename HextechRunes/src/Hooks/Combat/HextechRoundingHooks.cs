using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;

namespace HextechRunes;

/// <summary>
/// 把伤害 / 格挡的引擎取整从「向下取整」改为「四舍五入」(away-from-zero)，
/// 但**只作用于我方(玩家方)造成的伤害与玩家方获得的格挡**——怪物造成的伤害、怪物获得的格挡
/// 一律保持原版 floor，避免改动怪物的底层数值(如被虚弱/易伤后的攻击力计算)。
///
/// 实现只挂在两个带敌我信息的伤害/格挡枢纽上：
/// - <c>Hook.ModifyDamage</c>(全量结算)：仅当 <c>dealer</c> 是玩家方时四舍五入(显示与实际一致)。
/// - <c>Hook.ModifyBlock</c>：仅当 <c>target</c>(获得格挡者)是玩家方时四舍五入。
/// 不再 patch <see cref="Creature"/> 的 HP/格挡/治疗汇聚点——那些无法区分敌我，会波及怪物。
///
/// 装在 guarded 安装里，任何方法定位失败都只会跳过本功能、不影响其余 mod。
/// </summary>
internal static class HextechRoundingHooks
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			AccessTools.Method(typeof(Hook), nameof(Hook.ModifyDamage)),
			postfix: new HarmonyMethod(typeof(HextechRoundingHooks), nameof(ModifyDamagePostfix)));
		harmony.Patch(
			AccessTools.Method(typeof(Hook), nameof(Hook.ModifyBlock)),
			postfix: new HarmonyMethod(typeof(HextechRoundingHooks), nameof(ModifyBlockPostfix)));
	}

	private static decimal RoundHalfUp(decimal value)
	{
		return Math.Round(value, MidpointRounding.AwayFromZero);
	}

	private static bool IsPlayerSide(Creature? creature)
	{
		return creature != null && creature.Side == CombatSide.Player;
	}

	/// <summary>仅玩家方造成的全量结算伤害四舍五入；怪物伤害、无源伤害(如中毒)保持原版。</summary>
	private static void ModifyDamagePostfix(ref decimal __result, Creature? dealer, ModifyDamageHookType modifyDamageHookType)
	{
		if (modifyDamageHookType == ModifyDamageHookType.All && IsPlayerSide(dealer))
		{
			__result = RoundHalfUp(__result);
		}
	}

	/// <summary>仅玩家方获得的格挡四舍五入；怪物格挡保持原版(含其联机缩放)。</summary>
	private static void ModifyBlockPostfix(ref decimal __result, Creature target)
	{
		if (IsPlayerSide(target))
		{
			__result = RoundHalfUp(__result);
		}
	}
}
