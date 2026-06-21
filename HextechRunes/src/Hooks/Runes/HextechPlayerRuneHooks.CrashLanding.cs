using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

internal static partial class HextechPlayerRuneHooks
{
	private static bool CrashLandingOnPlayPrefix(CrashLanding __instance, PlayerChoiceContext choiceContext, ref Task __result)
	{
		if (!CrashLandingUpgradeRune.ShouldUseUpgradedPlay(__instance))
		{
			return true;
		}

		__result = CrashLandingUpgradeRune.PlayUpgraded(choiceContext, __instance);
		return false;
	}
}
