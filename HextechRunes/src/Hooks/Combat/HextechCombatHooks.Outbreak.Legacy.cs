#if !STS2_110_OR_NEWER
namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	// 0.110 之前疫情是持续监听中毒施加的 OutbreakPower；只守卫由持有者自己施加中毒触发的那次响应。
	[HarmonyPatch(typeof(OutbreakPower), nameof(OutbreakPower.AfterPowerAmountChanged), typeof(PlayerChoiceContext), typeof(PowerModel), typeof(decimal), typeof(Creature), typeof(CardModel))]
	[HextechPatch("combat.outbreak", "即死与补偿安全边界")]
	private static class OutbreakPatch
	{
		[HarmonyPrefix]
		private static void Prefix(OutbreakPower __instance, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource, out bool __state)
		{
			__state = amount > 0m
				&& applier == __instance.Owner
				&& power is PoisonPower;
			if (__state)
			{
				OutbreakPowerPoisonResponseGuard.Enter();
			}
		}

		[HarmonyPostfix]
		private static void Postfix(ref bool __state, ref Task __result) => WrapOutbreakResponse(ref __state, ref __result);

		[HarmonyFinalizer]
		private static Exception? Finalizer(bool __state, Exception? __exception) => ExitGuardAfterSynchronousFailure(OutbreakPowerPoisonResponseGuard, __state, __exception);
	}
}
#endif
