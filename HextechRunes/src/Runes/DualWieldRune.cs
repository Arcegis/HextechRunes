namespace HextechRunes;

public sealed class DualWieldRune : HextechRelicBase
{
	// 文案"伤害降低 40%"：乘区系数 0.6。
	private const decimal DamageMultiplier = 0.6m;

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		return IsOwnedAttack(card) ? playCount + 1 : playCount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (IsOwnedAttack(card))
		{
			Flash();
		}

		return Task.CompletedTask;
	}

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource) ? DamageMultiplier : 1m;
	}
}
