using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunes;

public sealed class MobileHomeRune : RelicBundleRuneBase
{
	private static readonly Type[] RelicTypes =
	[
		typeof(MeatCleaver),
		typeof(Shovel),
		typeof(Girya),
		typeof(MiniatureTent)
	];

	protected override IReadOnlyList<Type> BundledRelicTypes => RelicTypes;
}
