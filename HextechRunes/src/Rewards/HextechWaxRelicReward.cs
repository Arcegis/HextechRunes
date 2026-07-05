namespace HextechRunes;

internal sealed class HextechWaxRelicReward : RelicReward
{
	public HextechWaxRelicReward(RelicModel relic, Player player)
		: base(EnsureWax(relic), player)
	{
	}

	public override SerializableReward ToSerializable()
	{
		SerializableReward save = base.ToSerializable();
		// Relic rewards do not use the gold-stolen flag; reuse it as a wax marker.
		save.WasGoldStolenBack = true;
		return save;
	}

	private static RelicModel EnsureWax(RelicModel relic)
	{
		relic.IsWax = true;
		return relic;
	}
}
