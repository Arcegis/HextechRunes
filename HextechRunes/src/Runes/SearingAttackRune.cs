using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace HextechRunes;

public sealed class SearingAttackRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<SearingAttackCard>()
	];

	public override Task AfterObtained()
	{
		return AddCardCopiesToDeckOrHand<SearingAttackCard>(DynamicVars.Cards.IntValue);
	}
}
