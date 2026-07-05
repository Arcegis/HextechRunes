namespace HextechRunes;

public sealed class CurtainCallRune : HextechRelicBase
{
	internal const string RetainMarkerSavedPropertyName = "SavedCurtainCallRetainMarker";

	public override bool HasUponPickupEffect => true;

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
		if (Owner == null || card.Owner != Owner)
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
