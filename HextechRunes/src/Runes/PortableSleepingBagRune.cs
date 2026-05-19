using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
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

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. HoverTipFactory.FromRelic<RegalPillow>(),
		.. HoverTipFactory.FromRelic<TinyMailbox>(),
		.. HoverTipFactory.FromRelic<DreamCatcher>(),
		.. HoverTipFactory.FromRelic<StoneHumidifier>()
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
