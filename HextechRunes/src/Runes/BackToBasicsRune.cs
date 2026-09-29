namespace HextechRunes;

public sealed class BackToBasicsRune : HextechRelicBase, IHextechHealingMultiplierProvider
{
	// 治疗、格挡、伤害共用的倍率与手动出牌的费用上限；文案写的是字面值，改数值要同步九语言。
	private const decimal StatMultiplier = 1.4m;
	private const decimal BlockedEnergyCost = 3m;

	// 只限制手动出牌。自动打出(形态开局自动打出、原版自动打出效果等)放行,与敌方同名海克斯、卡卡同口径;
	// 否则同时持有"升级:XX形态"时,3 费形态牌会在开局被拦下直接进结果堆。
	public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
	{
		return autoPlayType != AutoPlayType.None
			|| card.Owner != Owner
			|| card.EnergyCost.CostsX
			|| HextechCombatHooks.GetEnergyCostForCurrentCardPlay(card) < BlockedEnergyCost;
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? StatMultiplier : 1m;
	}

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource) ? StatMultiplier : 1m;
	}

	decimal IHextechHealingMultiplierProvider.ModifyHealingMultiplicative(Player player, Creature creature, decimal amount)
	{
		return IsFirstOwnedInstance(player) ? StatMultiplier : 1m;
	}
}
