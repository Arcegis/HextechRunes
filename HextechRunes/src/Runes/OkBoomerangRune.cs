namespace HextechRunes;

public sealed class OkBoomerangRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<OkBoomerangCard>()
	];

	public override Task AfterObtained()
	{
		return AddCardCopiesToDeckOrHand<OkBoomerangCard>(DynamicVars.Cards.IntValue);
	}
}
