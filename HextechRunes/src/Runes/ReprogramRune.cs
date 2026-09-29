namespace HextechRunes;

public sealed class ReprogramRune : HextechRelicBase
{
	private const int PickupCardCopies = 2;

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
		await AddCardCopiesToDeckOrHand<ReprogramCard>(PickupCardCopies);
	}
}
