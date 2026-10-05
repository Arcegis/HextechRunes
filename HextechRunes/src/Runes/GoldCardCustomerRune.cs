using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunes;

public sealed class GoldCardCustomerRune : RelicBundleRuneBase
{
	private static readonly Type[] RelicTypes =
	[
		typeof(TheCourier),
		typeof(MembershipCard)
	];

	protected override IReadOnlyList<Type> BundledRelicTypes => RelicTypes;
}
