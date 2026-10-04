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
		bool free = ShouldBladeBeFree(card);
		modifiedCost = free ? 0m : originalCost;
		return free;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		bool free = ShouldBladeBeFree(card);
		modifiedCost = free ? 0m : originalCost;
		return free;
	}

	private bool ShouldBladeBeFree(CardModel card)
	{
		return card.Owner == Owner
			&& card is SovereignBlade
			&& card.Pile?.Type is PileType.Hand or PileType.Play;
	}
}
