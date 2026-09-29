using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunes;

public sealed class OrobasBlessingRune : HextechRelicBase
{
	private static readonly Type[] RelicTypes =
	[
		typeof(ArchaicTooth),
		typeof(TouchOfOrobas)
	];

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => BundledRelicHoverTips(RelicTypes);

	public override Task AfterObtained()
	{
		return Owner == null ? Task.CompletedTask : RelicBundleGrantHelper.GrantRelics(Owner, RelicTypes);
	}
}
