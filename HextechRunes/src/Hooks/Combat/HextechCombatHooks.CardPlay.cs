namespace HextechRunes;

internal static partial class HextechCombatHooks
{

	internal static UnplayableReason ResolveCardPlayAllowanceReasons(
		bool blueCandleAllows,
		bool grandFinaleAllows)
	{
		UnplayableReason allowedReasons = UnplayableReason.None;
		if (blueCandleAllows)
		{
			allowedReasons |= UnplayableReason.HasUnplayableKeyword;
		}
		if (grandFinaleAllows)
		{
			allowedReasons |= UnplayableReason.BlockedByCardLogic;
		}

		return allowedReasons;
	}

	internal static void ApplyCardPlayAllowance(
		ref bool result,
		ref UnplayableReason reason,
		bool hasPreventer,
		UnplayableReason allowedReasons)
	{
		if (result || reason == UnplayableReason.None)
		{
			return;
		}

		reason &= ~allowedReasons;
		result = reason == UnplayableReason.None && !hasPreventer;
	}


	private static bool IsBlockedByBackToBasics(CardModel card)
	{
		return IsBlockedByBackToBasics(card, out _);
	}

	private static bool IsBlockedByBackToBasics(CardModel card, out AbstractModel? preventer)
	{
		preventer = null;
		if (card.Owner == null)
		{
			return false;
		}

		// 玩家遗物「回归基本功」:费用 ≥ 3 的牌不可打出。
		BackToBasicsRune? rune = card.Owner.GetRelic<BackToBasicsRune>();
		if (rune != null
			&& !card.EnergyCost.CostsX
			&& GetEnergyCostForCurrentCardPlay(card) >= 3m)
		{
			preventer = rune;
			return true;
		}

		// 敌方海克斯「回归基本功」:每回合打出的牌数达到上限后,其余牌不可再打出。
		if (card.Owner.Creature.Side == CombatSide.Player
			&& card.Owner.Creature.CombatState?.RunState is RunState runState
			&& HextechMayhemModifier.FindIn(runState) is HextechMayhemModifier modifier
			&& modifier.HasActiveMonsterHex(MonsterHexKind.BackToBasics))
		{
			int limit = BackToBasicsEnemyHex.GetTurnCardLimit(modifier);
			int played = modifier.CombatTracking.BackToBasicsCardsPlayedThisTurn.GetValueOrDefault(card.Owner.NetId);
			if (played >= limit)
			{
				preventer = modifier;
				return true;
			}
		}

		return false;
	}

	[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlay), new Type[0])]
	[HextechPatch("combat.can-play", "禁玩判定")]
	private static class CanPlayPatch
	{
		[HarmonyPostfix]
		[HarmonyPriority(Priority.Last)]
		private static void Postfix(CardModel __instance, ref bool __result)
		{
			if (__result && IsBlockedByBackToBasics(__instance))
			{
				__result = false;
				return;
			}

			if (__result && KakaRune.BlocksAttack(__instance))
			{
				__result = false;
			}
		}
	}

	[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlay), new[] { typeof(UnplayableReason), typeof(AbstractModel) }, new[] { ArgumentType.Out, ArgumentType.Out })]
	[HextechPatch("combat.can-play-reason", "禁玩判定")]
	private static class CanPlayWithReasonPatch
	{
		[HarmonyPostfix]
		[HarmonyPriority(Priority.First)]
		private static void AllowancePostfix(
			CardModel __instance,
			ref bool __result,
			ref UnplayableReason reason,
			ref AbstractModel preventer)
		{
			UnplayableReason allowedReasons = ResolveCardPlayAllowanceReasons(
				BlueCandleMedkitRune.AllowsPlaying(__instance),
				GrandFinaleUpgradeRune.AllowsPlaying(__instance));
			ApplyCardPlayAllowance(ref __result, ref reason, preventer != null, allowedReasons);
		}

		[HarmonyPostfix]
		[HarmonyPriority(Priority.Last)]
		private static void BlockerPostfix(
			CardModel __instance,
			ref bool __result,
			ref UnplayableReason reason,
			ref AbstractModel preventer)
		{
			if (__result && IsBlockedByBackToBasics(__instance, out AbstractModel? backToBasicsPreventer))
			{
				reason |= UnplayableReason.BlockedByHook;
				preventer = backToBasicsPreventer!;
				__result = false;
				return;
			}

			if (__result
				&& KakaRune.BlocksAttack(__instance)
				&& __instance.Owner?.GetRelic<KakaRune>() is KakaRune kakaRune)
			{
				reason |= UnplayableReason.BlockedByHook;
				preventer = kakaRune;
				__result = false;
			}
		}
	}
}
