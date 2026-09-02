using MegaCrit.Sts2.Core.CardSelection;

namespace HextechRunes;

internal static partial class HextechPlayerRuneHooks
{


	[HarmonyPatch(typeof(BodySlam), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.body-slam", "升级铁山靠", Rune = typeof(BodySlamUpgradeRune))]
	private static class BodySlamPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(
			BodySlam __instance,
			PlayerChoiceContext choiceContext,
			CardPlay cardPlay,
			ref Task __result)
		{
			if (__instance.Owner?.GetRelic<BodySlamUpgradeRune>() is not BodySlamUpgradeRune rune)
			{
				return true;
			}

			__result = rune.PlayUpgraded(choiceContext, __instance, cardPlay);
			return false;
		}
	}

	[HarmonyPatch(typeof(WroughtInWar), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.wrought-in-war", "升级战火淬炼", Rune = typeof(WroughtInWarUpgradeRune))]
	private static class WroughtInWarPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(
			WroughtInWar __instance,
			PlayerChoiceContext choiceContext,
			CardPlay cardPlay,
			ref Task __result)
		{
			if (__instance.Owner?.GetRelic<WroughtInWarUpgradeRune>() is not WroughtInWarUpgradeRune rune)
			{
				return true;
			}

			__result = rune.PlayUpgraded(choiceContext, __instance, cardPlay);
			return false;
		}
	}

	[HarmonyPatch(typeof(DecisionsDecisions), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.decisions.play", "升级抉择", Rune = typeof(DecisionsDecisionsUpgradeRune))]
	private static class DecisionsDecisionsOnPlayPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(
			DecisionsDecisions __instance,
			PlayerChoiceContext choiceContext,
			ref Task __result)
		{
			if (__instance.Owner?.GetRelic<DecisionsDecisionsUpgradeRune>() is not DecisionsDecisionsUpgradeRune rune)
			{
				return true;
			}

			__result = rune.PlayUpgraded(choiceContext, __instance);
			return false;
		}
	}

	[HarmonyPatch(typeof(CardSelectCmd), nameof(CardSelectCmd.FromHand), typeof(PlayerChoiceContext), typeof(Player), typeof(CardSelectorPrefs), typeof(Func<CardModel, bool>), typeof(AbstractModel))]
	[HextechPatch("rune.decisions.select", "升级抉择", Rune = typeof(DecisionsDecisionsUpgradeRune))]
	private static class DecisionsDecisionsFromHandPatch
	{
		[HarmonyPrefix]
		private static void Prefix(
			AbstractModel source,
			ref Func<CardModel, bool> filter)
		{
			if (source is not DecisionsDecisions card
				|| card.Owner?.GetRelic<DecisionsDecisionsUpgradeRune>() is not DecisionsDecisionsUpgradeRune rune)
			{
				return;
			}

			rune.Flash();
			filter = DecisionsDecisionsUpgradeRune.CanSelectCard;
		}
	}
}
