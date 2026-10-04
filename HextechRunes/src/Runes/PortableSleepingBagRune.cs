using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunes;

public sealed class PortableSleepingBagRune : RelicBundleRuneBase
{
	private static readonly Type[] RelicTypes =
	[
		typeof(RegalPillow),
		typeof(TinyMailbox),
		typeof(DreamCatcher),
		typeof(StoneHumidifier)
	];

	protected override IReadOnlyList<Type> BundledRelicTypes => RelicTypes;
}
