namespace HextechRunes;

/// <summary>
/// 升级：散射炮——散射炮也计入消耗牌堆里的状态牌。原版只数不在消耗牌堆的状态牌并把它们消耗；
/// 持有者的命中数改为全部状态牌，已在消耗牌堆的那些不再重复消耗。
/// </summary>
public sealed class FlakCannonUpgradeRune : CardUpgradeRuneBase<FlakCannon>
{
	protected override bool IsAvailableForCharacter(Player player) => IsDefectPlayer(player);

	private static bool IsUpgradedFor(Player? owner) => owner?.GetRelic<FlakCannonUpgradeRune>() != null;

	internal static CardModel[] GetStatusesIncludingExhaust(PlayerCombatState playerCombatState) => playerCombatState.AllCards
		.Where(static card => card.Type == CardType.Status).ToArray();

	internal static CardModel[] GetStatusesToExhaust(PlayerCombatState playerCombatState) => playerCombatState.AllCards
		.Where(static card => card.Type == CardType.Status && card.Pile?.Type != PileType.Exhaust).ToArray();

	internal static async Task PlayUpgraded(PlayerChoiceContext context, FlakCannon card, CardPlay play)
	{
		if (card.CombatState is not { } combatState || card.Owner.PlayerCombatState is not { } playerCombatState)
		{
			return;
		}

		CardModel[] statuses = GetStatusesToExhaust(playerCombatState);
		// 命中数走原版 CalculatedVar（其状态牌来源已由下方补丁扩展到消耗牌堆），与卡面显示一致。
		int hits = (int)((CalculatedVar)card.DynamicVars["CalculatedHits"]).Calculate(play.Target);
		foreach (CardModel status in statuses)
		{
			await CardCmd.Exhaust(context, status);
		}

		await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue).WithHitCount(hits)
			.FromCardCompat(card, play).TargetingRandomOpponents(combatState)
			.WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3").Execute(context);
	}

	// 原版命中数与卡面显示都经私有的 GetStatuses 取状态牌，持有者改为包含消耗牌堆。
	[HarmonyPatch(typeof(FlakCannon), "GetStatuses", typeof(Player))]
	[HextechPatch("rune.flak-cannon.exhausted-statuses", "升级散射炮", Rune = typeof(FlakCannonUpgradeRune))]
	private static class FlakCannonStatusesPatch
	{
		[HarmonyPostfix]
		private static void Postfix(Player owner, ref IEnumerable<CardModel> __result)
		{
			if (IsUpgradedFor(owner) && owner.PlayerCombatState is { } playerCombatState)
			{
				__result = GetStatusesIncludingExhaust(playerCombatState);
			}
		}
	}

	// 跳过型前缀：原版 OnPlay 会对 GetStatuses 返回的每张牌调用 CardCmd.Exhaust，扩展后会把消耗牌堆里的
	// 状态牌再消耗一次（重复触发消耗类效果）。只对持有者替换为"只消耗不在消耗牌堆的状态牌"，其余照原版。
	[HarmonyPatch(typeof(FlakCannon), "OnPlay", typeof(PlayerChoiceContext), typeof(CardPlay))]
	[HextechPatch("rune.flak-cannon.play", "升级散射炮", Rune = typeof(FlakCannonUpgradeRune))]
	private static class FlakCannonPlayPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(Priority.Low)]
		private static bool Prefix(FlakCannon __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
		{
			if (!IsUpgradedFor(__instance.Owner))
			{
				return true;
			}

			__result = PlayUpgraded(choiceContext, __instance, cardPlay);
			return false;
		}
	}
}
