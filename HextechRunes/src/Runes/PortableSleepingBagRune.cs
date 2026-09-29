using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunes;

public sealed class PortableSleepingBagRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	private static readonly Type[] RelicTypes =
	[
		typeof(RegalPillow),
		typeof(TinyMailbox),
		typeof(DreamCatcher),
		typeof(StoneHumidifier)
	];

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
