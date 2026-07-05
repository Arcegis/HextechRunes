namespace HextechRunes;

public sealed class EnlightenmentRune : HextechRelicBase
{
	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (Owner == null
			|| card.Owner != Owner
			|| card.EnergyCost.CostsX
			|| originalCost <= 1m)
		{
			return false;
		}

		modifiedCost = 1m;
		return true;
	}
}
