using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunes;

public sealed class OrobasBlessingRune : RelicBundleRuneBase
{
	private static readonly Type[] RelicTypes =
	[
		typeof(ArchaicTooth),
		typeof(TouchOfOrobas)
	];

	protected override IReadOnlyList<Type> BundledRelicTypes => RelicTypes;

	protected override bool FlashOnObtain => false;
}
