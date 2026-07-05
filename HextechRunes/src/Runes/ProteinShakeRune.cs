namespace HextechRunes;

public sealed class ProteinShakeRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("MaxHpPerStep", 2m),
		new DynamicVar("SustainPercentPerStep", 1m)
	];

	public decimal SustainMultiplier => Owner == null
		? 1m
		: 1m + Math.Floor(Owner.Creature.MaxHp / DynamicVars["MaxHpPerStep"].BaseValue) * DynamicVars["SustainPercentPerStep"].BaseValue / 100m;

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? SustainMultiplier : 1m;
	}
}
