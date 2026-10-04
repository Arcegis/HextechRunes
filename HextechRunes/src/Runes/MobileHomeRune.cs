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

	protected override IEnumerable<IHoverTip> ExtraHoverTips => BundledRelicHoverTips(RelicTypes);

	public override async Task AfterObtained()
	{
		Flash();
		await RelicBundleGrantHelper.GrantRelics(Owner, RelicTypes);
	}
}
