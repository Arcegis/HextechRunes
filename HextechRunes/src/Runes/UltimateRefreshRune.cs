namespace HextechRunes;

public sealed class UltimateRefreshRune : HextechRelicBase
{
	private const decimal MinEffectiveCost = 2m;

	// 旧版本存档兼容占位：原为瞬时闪光标记，已不再使用；名称与类型须保留。
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public bool SavedTriggeredThisTurn
	{
		get => false;
		set { }
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		return IsOwnedCardWithEffectiveCostAtLeast(card, MinEffectiveCost) ? playCount + 1 : playCount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (IsOwnedCardWithEffectiveCostAtLeast(card, MinEffectiveCost))
		{
			Flash();
		}

		return Task.CompletedTask;
	}
}
