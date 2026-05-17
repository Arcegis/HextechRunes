namespace HextechRunes;

internal sealed class BigStrengthEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.BigStrength;

	internal override decimal ModifyDamageMultiplicative(HextechEnemyHexContext context, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return 1.2m;
	}
}
