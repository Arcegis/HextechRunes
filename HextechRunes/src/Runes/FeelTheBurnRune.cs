namespace HextechRunes;

public sealed class FeelTheBurnRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<FeelTheBurnCard>()
	];

	public override Task AfterObtained()
	{
		return AddCardCopiesToDeckOrHand<FeelTheBurnCard>(DynamicVars.Cards.IntValue);
	}
}
