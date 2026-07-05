namespace HextechRunes;

public sealed class BackToBasicsRune : HextechRelicBase
{
	public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
	{
		return card.Owner != Owner
			|| card.EnergyCost.CostsX
			|| HextechCombatHooks.GetEnergyCostForCurrentCardPlay(card) < 3m;
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? 1.4m : 1m;
	}

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource) ? 1.4m : 1m;
	}
}
