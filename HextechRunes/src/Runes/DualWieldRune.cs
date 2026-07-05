namespace HextechRunes;

public sealed class DualWieldRune : HextechRelicBase
{
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
		return IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource) ? 0.6m : 1m;
	}
}
