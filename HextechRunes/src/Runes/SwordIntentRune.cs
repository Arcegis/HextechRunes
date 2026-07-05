namespace HextechRunes;

public sealed class SwordIntentRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<SovereignBlade>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldBladeBeFree(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldBladeBeFree(card))
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	private bool ShouldBladeBeFree(CardModel card)
	{
		return Owner != null
			&& card.Owner == Owner
			&& card is SovereignBlade
			&& card.Pile?.Type is PileType.Hand or PileType.Play;
	}
}
