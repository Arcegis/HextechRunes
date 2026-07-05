namespace HextechRunes;

public abstract class DragonSoulRuneBase<TCard> : HextechRelicBase
	where TCard : CardModel
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<TCard>()
	];

	public override async Task AfterObtained()
	{
		Flash();
		await AddCardCopiesToDeckOrHand<TCard>(DynamicVars.Cards.IntValue);
	}
}
