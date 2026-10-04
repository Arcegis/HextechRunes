namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	// 跳过型前缀（已裁决保留，见 architecture.md）：原版 GetTypeForAmount 对 Counter+AllowNegative 的负层数一律判 Debuff，
	// 且不是虚方法、没有 Hook。只对本模组的两种缓慢能力返回 None（缓慢的正负层只表达受伤倍率方向），
	// 其余能力走原版。目标 IL 由原版拷贝守卫冻结。
	[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.GetTypeForAmount), typeof(decimal))]
	[HextechPatch("combat.power-type-for-amount", "中性能力类型")]
	private static class PowerTypeForAmountPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(
			PowerModel __instance,
			ref PowerType __result)
		{
			if (__instance is not (HextechPlayerSlowPower or HextechTemporarySlowPower))
			{
				return true;
			}

			__result = PowerType.None;
			return false;
		}
	}
}
