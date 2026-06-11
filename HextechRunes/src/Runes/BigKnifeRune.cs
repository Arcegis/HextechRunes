using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

public sealed class BigKnifeRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Shiv>(),
		HoverTipFactory.FromCard<SovereignBlade>(),
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldMakeBladeFree(card) || card.EnergyCost.CostsX)
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (!ShouldMakeBladeFree(card) || card.HasStarCostX)
		{
			return false;
		}

		modifiedCost = 0m;
		return true;
	}

	private bool ShouldMakeBladeFree(CardModel card)
	{
		return Owner != null
			&& card.Owner == Owner
			&& card is SovereignBlade
			&& card.Pile?.Type is PileType.Hand or PileType.Play;
	}
}
