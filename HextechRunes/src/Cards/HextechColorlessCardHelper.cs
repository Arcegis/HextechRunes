using MegaCrit.Sts2.Core.Models.CardPools;

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
