namespace HextechRunes;

public sealed class TapDanceRune : HextechRelicBase
{
	private const decimal CardsDrawnPerAttack = 1m;

	// 旧版本存档兼容占位：原为待抽牌计数，已不再使用；名称与类型须保留。
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedPendingDraw
	{
		get => 0;
		set { }
	}

	public override bool ShowCounter => false;

	public override int DisplayAmount => 0;

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null || !IsOwnedAttack(cardPlay.Card))
		{
			return Task.CompletedTask;
		}

		Flash();
		return CardPileCmd.Draw(context, CardsDrawnPerAttack, Owner, fromHandDraw: false);
	}
}
