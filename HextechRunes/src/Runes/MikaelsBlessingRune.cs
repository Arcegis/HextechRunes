namespace HextechRunes;

public sealed class MikaelsBlessingRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<MikaelsBlessingCard>()
	];

	public override Task AfterObtained()
	{
		return AddCardCopiesToDeckOrHand<MikaelsBlessingCard>(DynamicVars.Cards.IntValue);
	}
}
