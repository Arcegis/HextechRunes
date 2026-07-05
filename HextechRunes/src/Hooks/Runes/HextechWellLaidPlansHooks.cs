using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

// 升级：计划妥当 —— 把 WellLaidPlansPower 回合结束保留手牌的可选上限从 Amount 放开到手牌数(=保留任意张)。
internal static class HextechWellLaidPlansHooks
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(WellLaidPlansPower), nameof(WellLaidPlansPower.BeforeFlushLate), BindingFlags.Instance | BindingFlags.Public, typeof(PlayerChoiceContext), typeof(Player)),
			prefix: new HarmonyMethod(typeof(HextechWellLaidPlansHooks), nameof(BeforeFlushLatePrefix)));
		HextechLog.Info($"[{ModInfo.Id}][WellLaidPlans] Unlimited retain hook installed.");
	}

	private static bool BeforeFlushLatePrefix(WellLaidPlansPower __instance, PlayerChoiceContext choiceContext, Player player, ref Task __result)
	{
		if (__instance.Owner?.Player == player && player?.GetRelic<WellLaidPlansUpgradeRune>() != null)
		{
			__result = UnlimitedRetain(__instance, choiceContext, player);
			return false;
		}

		return true;
	}

	private static async Task UnlimitedRetain(WellLaidPlansPower power, PlayerChoiceContext choiceContext, Player player)
	{
		if (!Hook.ShouldFlush(player.Creature.CombatState, player))
		{
			return;
		}

		int handCount = PileType.Hand.GetPile(player).Cards.Count();
		if (handCount <= 0)
		{
			return;
		}

		// SelectionScreenPrompt 在 PowerModel 上是 protected,用 Traverse 反射读取原版「保留」提示文案。
		LocString prompt = Traverse.Create(power).Property("SelectionScreenPrompt").GetValue<LocString>();
		List<CardModel> selected = (await CardSelectCmd.FromHand(
			prefs: new CardSelectorPrefs(prompt, 0, handCount),
			context: choiceContext,
			player: player,
			filter: static card => !card.ShouldRetainThisTurn,
			source: power)).ToList();
		foreach (CardModel card in selected)
		{
			card.GiveSingleTurnRetain();
		}
	}
}
