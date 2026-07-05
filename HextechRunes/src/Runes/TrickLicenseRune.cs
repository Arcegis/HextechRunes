namespace HextechRunes;

public sealed class TrickLicenseRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Sly)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldPlayForFree(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldPlayForFree(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	private bool ShouldPlayForFree(CardModel card)
	{
		return Owner != null
			&& card.Owner == Owner
			&& card.IsSlyThisTurn
			&& card.Pile?.Type is PileType.Hand or PileType.Play;
	}
}
