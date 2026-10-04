namespace HextechRunes;

public sealed class CurtainCallRune : HextechRelicBase
{
	internal const string RetainMarkerSavedPropertyName = nameof(SavedCurtainCallRetainMarker);

	public override bool HasUponPickupEffect => true;

	// 只为在 SavedProperty 名称表里登记这个名字，供卡牌存档写入关键词标记(见 HextechThoughtOverwriteKeywordPersistenceHooks)；自身不存值，名称与类型须保留。
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	private int SavedCurtainCallRetainMarker
	{
		get => 0;
		set { }
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<GrandFinale>(),
		HoverTipFactory.FromKeyword(CardKeyword.Retain)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterObtained()
	{
		Flash();
		await AddCardCopiesToDeckOrHand<GrandFinale>(
			DynamicVars.Cards.IntValue,
			static card =>
			{
				CurtainCallKeywordPersistence.Track(card);
				CardCmd.ApplyKeyword(card, CardKeyword.Retain);
			});
	}

	public override Task AfterCardEnteredCombat(CardModel card)
	{
		if (card.Owner != Owner)
		{
			return Task.CompletedTask;
		}

		if (CurtainCallKeywordPersistence.IsTracked(card.DeckVersion))
		{
			CurtainCallKeywordPersistence.Restore(card);
		}

		return Task.CompletedTask;
	}
}
