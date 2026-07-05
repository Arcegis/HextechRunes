namespace HextechRunes;

public sealed class BladeWaltzRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<BladeWaltzCard>()
	];

	public override async Task AfterObtained()
	{
		Flash();
		await AddCardCopiesToDeckOrHand<BladeWaltzCard>(DynamicVars.Cards.IntValue);
	}
}
