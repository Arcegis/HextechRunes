using MegaCrit.Sts2.Core.CardSelection;

namespace HextechRunes;

internal static partial class HextechCombatHooks
{
	private static async Task SafeEntropyAfterPlayerTurnStart(EntropyPower entropyPower, PlayerChoiceContext choiceContext, Player player)
	{
		if (player != entropyPower.Owner.Player)
		{
			return;
		}

		IEnumerable<CardModel> selected = await CardSelectCmd.FromHand(
			choiceContext,
			player,
			new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, entropyPower.Amount),
			CardTransformUpgradeHelper.CanTransformToRandomCard,
			entropyPower);

		List<CardModel> selectedCards = selected.ToList();
		for (int i = 0; i < selectedCards.Count; i++)
		{
			CardModel card = selectedCards[i];
			if (CardTransformUpgradeHelper.CanTransformToRandomCard(card))
			{
				await CardTransformUpgradeHelper.TransformToStableRandom(
					card,
					(RunState)player.RunState,
					"entropy-transform-replacement",
					i,
					saltParts:
					[
						HextechStableRandom.PlayerKey(player),
						player.Creature.CombatState?.RoundNumber.ToString() ?? "-1",
						entropyPower.Amount.ToString(),
						HextechStableRandom.CardPileKey(selectedCards)
					]);
			}
		}
	}

	// 升级雷暴由 Modifier 的出牌事件（HextechMayhem.CardEvents）补发闪电，持有者的原版雷暴出牌前/后回调都要跳过；
	// 两个补丁 ID 分别保留，只共用方法体。
	private static bool RunStormCallbackUnlessUpgraded(StormPower storm, ref Task result)
	{
		Player? owner = storm.Owner?.Player;
		if (HextechMayhemModifier.FindIn(owner?.Creature.CombatState?.RunState) == null
			|| owner?.GetRelic<StormUpgradeRune>() == null)
		{
			return true;
		}

		result = Task.CompletedTask;
		return false;
	}

	[HarmonyPatch(typeof(StormPower), nameof(StormPower.BeforeCardPlayed), typeof(CardPlay))]
	[HextechPatch("combat.storm.before-card-played", "升级雷暴")]
	private static class StormBeforeCardPlayedPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(StormPower __instance, ref Task __result) => RunStormCallbackUnlessUpgraded(__instance, ref __result);
	}

	[HarmonyPatch(typeof(StormPower), nameof(StormPower.AfterCardPlayed), typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("combat.storm.after-card-played", "升级雷暴")]
	private static class StormAfterCardPlayedPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(StormPower __instance, ref Task __result) => RunStormCallbackUnlessUpgraded(__instance, ref __result);
	}

	[HarmonyPatch(typeof(EntropyPower), nameof(EntropyPower.AfterPlayerTurnStart), typeof(PlayerChoiceContext), typeof(Player))]
	[HextechPatch("combat.entropy.turn-start", "神秘符文")]
	private static class EntropyTurnStartPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(EntropyPower __instance, PlayerChoiceContext choiceContext, Player player, ref Task __result)
		{
			if (__instance.Owner?.Player?.GetRelic<MysteryRune>() == null)
			{
				return true;
			}

			__result = SafeEntropyAfterPlayerTurnStart(__instance, choiceContext, player);
			return false;
		}
	}
}
