using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

public sealed class NonupeipeGenerosityRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		RelicModel relic = HextechAncientRelicHelper.CreateRandomNonupeipeRelic(Owner, "nonupeipe-generosity");
		SaveManager.Instance.MarkRelicAsSeen(relic);
		Flash();
		await RelicCmd.Obtain(relic, Owner);
	}
}
