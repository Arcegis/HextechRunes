using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

internal static class HextechColorlessCardHelper
{
	public static bool IsColorlessCard(CardModel card)
	{
		return HextechRegentGeneratedCardHelper.IsAllowedGeneratedCard(card)
			|| card.Pool is ColorlessCardPool
			|| card.VisualCardPool is ColorlessCardPool;
	}
}
