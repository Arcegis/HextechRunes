namespace HextechRunes;

public sealed class MoreTheMerrierRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("PercentPerRelic", 1.5m)
	];

	public decimal SustainMultiplier => 1m + CountRelics() * DynamicVars["PercentPerRelic"].BaseValue / 100m;

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? SustainMultiplier : 1m;
	}

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource) ? SustainMultiplier : 1m;
	}

	private int CountRelics()
	{
		return Owner?.Relics.Count ?? 0;
	}
}
