namespace HextechRunes;

public sealed class ReprogramRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<ReprogramCard>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Flash();
		await AddCardCopiesToDeckOrHand<ReprogramCard>(2);
	}
}
