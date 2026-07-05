namespace HextechRunes;

public sealed class BigHandsRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Multiplier", 2m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override decimal ModifySummonAmount(Player summoner, decimal amount, AbstractModel? source)
	{
		return summoner == Owner ? amount * DynamicVars["Multiplier"].BaseValue : amount;
	}
}
