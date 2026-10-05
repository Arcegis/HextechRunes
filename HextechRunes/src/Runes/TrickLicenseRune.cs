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
		bool free = ShouldPlayForFree(card);
		modifiedCost = free ? 0m : originalCost;
		return free;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		bool free = ShouldPlayForFree(card);
		modifiedCost = free ? 0m : originalCost;
		return free;
	}

	private bool ShouldPlayForFree(CardModel card)
	{
		return card.Owner == Owner
			&& card.IsSlyThisTurn
			&& card.Pile?.Type is PileType.Hand or PileType.Play;
	}
}
