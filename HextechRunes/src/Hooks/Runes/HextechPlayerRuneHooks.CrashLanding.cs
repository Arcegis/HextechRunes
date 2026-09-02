namespace HextechRunes;

internal static partial class HextechPlayerRuneHooks
{

	[HarmonyPatch(typeof(CrashLanding), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.crash-landing", "升级迫降", Rune = typeof(CrashLandingUpgradeRune))]
	private static class CrashLandingPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(CrashLanding __instance, PlayerChoiceContext choiceContext, ref Task __result)
		{
			if (!CrashLandingUpgradeRune.ShouldUseUpgradedPlay(__instance))
			{
				return true;
			}

			__result = CrashLandingUpgradeRune.PlayUpgraded(choiceContext, __instance);
			return false;
		}
	}
}
