namespace HextechRunes;

public sealed class HeavyHitterRune : HextechRelicBase
{
	// 每 6 点最大生命值 +1% 伤害。
	private const decimal MaxHpPerDamagePercent = 6m;

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (!IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource))
		{
			return 1m;
		}

		return 1m + Math.Floor(Owner.Creature.MaxHp / MaxHpPerDamagePercent) / 100m;
	}
}
