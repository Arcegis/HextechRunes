namespace HextechRunes;

internal sealed class HeavyHitterEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.HeavyHitter;

	internal override decimal ModifyDamageMultiplicative(HextechEnemyHexContext context, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return dealer == null ? 1m : 1m + Math.Min(30m, Math.Floor(dealer.MaxHp / 10m)) / 100m;
	}
}
