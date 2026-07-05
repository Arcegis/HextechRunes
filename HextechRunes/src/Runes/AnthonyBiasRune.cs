namespace HextechRunes;

public sealed class AnthonyBiasRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("PercentPerCard", 1m)
	];

	public decimal SustainMultiplier => 1m + CountDeckCards() * DynamicVars["PercentPerCard"].BaseValue / 100m;

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? SustainMultiplier : 1m;
	}

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource) ? SustainMultiplier : 1m;
	}

	private int CountDeckCards()
	{
		return Owner?.Deck.Cards.Count ?? 0;
	}
}
