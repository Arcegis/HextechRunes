namespace HextechRunes;

public sealed class UnyieldingArmorRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<PlatingPower>()
	];

	public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
	{
		modifiedAmount = amount;
		if (Owner == null || target != Owner.Creature || canonicalPower is not PlatingPower || amount >= 0m)
		{
			return false;
		}

		modifiedAmount = 0m;
		Flash();
		return true;
	}
}
