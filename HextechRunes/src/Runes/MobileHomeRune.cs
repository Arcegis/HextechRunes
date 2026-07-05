using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunes;

public sealed class MobileHomeRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	private static readonly Type[] RelicTypes =
	[
		typeof(MeatCleaver),
		typeof(Shovel),
		typeof(Girya),
		typeof(MiniatureTent)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. HoverTipFactory.FromRelic<MeatCleaver>(),
		.. HoverTipFactory.FromRelic<Shovel>(),
		.. HoverTipFactory.FromRelic<Girya>(),
		.. HoverTipFactory.FromRelic<MiniatureTent>()
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		Flash();
		await RelicBundleGrantHelper.GrantRelics(Owner, RelicTypes);
	}
}
