using MegaCrit.Sts2.Core.Models.CardPools;

namespace HextechRunes;

internal static class HextechColorlessCardHelper
{
	// 本体符文（无瑕、空白支票、三棱镜等）的「无色牌」口径：无色牌池的牌，另把摄政王的衍生牌
	// （君王之剑与三张仆从牌）也按无色计。
	public static bool IsColorlessCard(CardModel card)
	{
		return IsRegentGeneratedCard(card)
			|| card.Pool is ColorlessCardPool
			|| card.VisualCardPool is ColorlessCardPool;
	}

	private static bool IsRegentGeneratedCard(CardModel card)
	{
		return card is SovereignBlade or MinionStrike or MinionDiveBomb or MinionSacrifice;
	}
}
