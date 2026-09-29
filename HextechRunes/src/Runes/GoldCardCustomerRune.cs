using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunes;

public sealed class GoldCardCustomerRune : HextechRelicBase
{
	private static readonly Type[] RelicTypes =
	[
		typeof(TheCourier),
		typeof(MembershipCard)
	];

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => BundledRelicHoverTips(RelicTypes);

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
