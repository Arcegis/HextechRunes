namespace HextechRunes;

public sealed class UniversalSpiral : EnchantmentModel
{
	private const string TimesKey = "Times";

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new IntVar(TimesKey, 1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.Static(StaticHoverTip.ReplayDynamic, DynamicVars[TimesKey])
	];

	public override bool CanEnchant(CardModel card)
	{
		return base.CanEnchant(card);
	}

	public override int EnchantPlayCount(int originalPlayCount)
	{
		return originalPlayCount + DynamicVars[TimesKey].IntValue;
	}
}
