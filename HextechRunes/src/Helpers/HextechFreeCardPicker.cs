namespace HextechRunes;

/// <summary>
/// "随机一张手牌本回合免费"的候选分级，与原版木乃伊之手（MummifiedHand.AfterCardPlayed，0.111.0）同序：
/// 基础费 &gt; 0 且计入全局修正后仍要花费 → 计入全局修正后仍要花费 → 基础费 &gt; 0 → 任意手牌。
/// 已被全局修正（三头犬、奇巧许可等 Hook.ModifyEnergyCostInCombat）变成免费的牌排在后两级，不会先把免费浪费掉。
/// X 费按原版口径：<c>CostsEnergyOrStars(includeGlobalModifiers: true)</c> 不认 X 费，只会落到后两级。
/// 只对非空的一级调用 pickFromTier；分级名进调用方的稳定随机键，改名会改变随机结果。
/// </summary>
internal static class HextechFreeCardPicker
{
	internal const string BaseCostTier = "base-cost";

	internal const string GlobalCostTier = "global-cost";

	internal const string BaseAnyTier = "base-any";

	internal const string AnyTier = "any";

	internal static CardModel? Pick(IReadOnlyList<CardModel> handCards, Func<IReadOnlyList<CardModel>, string, CardModel?> pickFromTier)
	{
		return Pick(
			handCards,
			HasBaseCost,
			static card => card.CostsEnergyOrStars(includeGlobalModifiers: true),
			pickFromTier);
	}

	// 分级逻辑与卡牌费用读取分开，便于不搭战斗环境直接验证分级顺序。
	internal static T? Pick<T>(
		IReadOnlyList<T> handCards,
		Func<T, bool> hasBaseCost,
		Func<T, bool> costsAfterGlobalModifiers,
		Func<IReadOnlyList<T>, string, T?> pickFromTier)
		where T : class
	{
		return PickTier(handCards.Where(card => hasBaseCost(card) && costsAfterGlobalModifiers(card)).ToList(), BaseCostTier, pickFromTier)
			?? PickTier(handCards.Where(costsAfterGlobalModifiers).ToList(), GlobalCostTier, pickFromTier)
			?? PickTier(handCards.Where(hasBaseCost).ToList(), BaseAnyTier, pickFromTier)
			?? PickTier(handCards.ToList(), AnyTier, pickFromTier);
	}

	internal static bool HasBaseCost(CardModel card)
	{
		return card.EnergyCost.GetWithModifiers(CostModifiers.None) > 0 || card.BaseStarCost > 0;
	}

	private static T? PickTier<T>(List<T> candidates, string tier, Func<IReadOnlyList<T>, string, T?> pickFromTier)
		where T : class
	{
		return candidates.Count == 0 ? null : pickFromTier(candidates, tier);
	}
}
