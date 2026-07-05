namespace HextechRunes;

public sealed class FirstAidKitRune : HextechRelicBase
{
	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? 1.25m : 1m;
	}
}
