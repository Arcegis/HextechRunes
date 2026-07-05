namespace HextechRunes;

public sealed class AllInRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<AllInCard>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterObtained()
	{
		Flash();
		await AddCardCopiesToDeckOrHand<AllInCard>(DynamicVars.Cards.IntValue);
	}
}
