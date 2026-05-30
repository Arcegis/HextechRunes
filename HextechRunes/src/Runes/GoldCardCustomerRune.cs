using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
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

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. HoverTipFactory.FromRelic<TheCourier>(),
		.. HoverTipFactory.FromRelic<MembershipCard>()
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
