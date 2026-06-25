using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;

namespace HextechRunes;

/// <summary>
/// 把全游戏伤害 / 格挡 / 治疗的引擎取整从「向下取整」改为「四舍五入」(away-from-zero)。
///
/// 原版在 <see cref="Creature"/> 的几个汇聚点用 <c>(int)</c> 截断 decimal(对正数即 floor),
/// 导致小数乘区(如 +12% 伤害)在小数值上被抹掉。这里:
/// 1) 在伤害/格挡的结算枢纽 <c>Hook.ModifyDamage</c>(仅全量结算)/<c>Hook.ModifyBlock</c> 对结果四舍五入,
///    让「显示预览」与「实际结算」一致地取整;
/// 2) 在 <see cref="Creature"/> 的应用汇聚点把入参先四舍五入成整数,覆盖中毒/灼烧/治疗/直接调用等所有路径。
///
/// 影响全游戏(原版+模组、玩家+敌人双向)。装在 guarded 安装里,任何方法定位失败都只会跳过本功能、不影响其余 mod。
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

		// 这些 internal 汇聚点入参都叫 amount，复用同一个四舍五入前缀。
		foreach (string methodName in new[]
		{
			nameof(Creature.LoseHpInternal),
			nameof(Creature.DamageBlockInternal),
			nameof(Creature.GainBlockInternal),
			nameof(Creature.LoseBlockInternal),
			nameof(Creature.SetCurrentHpInternal),
		})
		{
			harmony.Patch(
				AccessTools.Method(typeof(Creature), methodName),
				prefix: new HarmonyMethod(typeof(HextechRoundingHooks), nameof(RoundAmountPrefix)));
		}
	}

	private static decimal RoundHalfUp(decimal value)
	{
		return Math.Round(value, MidpointRounding.AwayFromZero);
	}

	/// <summary>只在全量结算(All)时四舍五入最终伤害，避免对仅加法/仅乘法等中间步骤提前取整。</summary>
	private static void ModifyDamagePostfix(ref decimal __result, ModifyDamageHookType modifyDamageHookType)
	{
		if (modifyDamageHookType == ModifyDamageHookType.All)
		{
			__result = RoundHalfUp(__result);
		}
	}

	private static void ModifyBlockPostfix(ref decimal __result)
	{
		__result = RoundHalfUp(__result);
	}

	/// <summary>把 Creature 汇聚点的 decimal 入参先四舍五入成整数，随后原版的 (int) 截断即等于四舍五入结果。</summary>
	private static void RoundAmountPrefix(ref decimal amount)
	{
		amount = RoundHalfUp(amount);
	}
}
