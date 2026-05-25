using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

internal static class HextechRegentGeneratedCardHelper
{
	public static bool IsAllowedGeneratedCard(CardModel card)
	{
		return card is SovereignBlade or MinionStrike or MinionDiveBomb or MinionSacrifice;
	}
}
