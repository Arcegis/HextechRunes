namespace HextechRunes;

public sealed class HastyScribbleRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(5)
	];

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return player == Owner ? count + DynamicVars.Cards.BaseValue : count;
	}
}
