namespace HextechRunes;

public sealed class UltimateRefreshRune : HextechRelicBase
{
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTriggeredThisTurn
	{
		get => false;
		set
		{
			// Legacy save compatibility: this was a transient flash flag.
		}
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (card.Owner != Owner)
		{
			return playCount;
		}

		if (!IsOwnedCardWithEffectiveCostAtLeast(card, 2m))
		{
			return playCount;
		}

		return playCount + 1;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (IsOwnedCardWithEffectiveCostAtLeast(card, 2m))
		{
			Flash();
		}

		return Task.CompletedTask;
	}
}
