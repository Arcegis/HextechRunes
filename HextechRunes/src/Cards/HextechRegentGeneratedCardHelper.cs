namespace HextechRunes;

internal static class HextechRegentGeneratedCardHelper
{
	public static bool IsAllowedGeneratedCard(CardModel card)
	{
		return card is SovereignBlade or MinionStrike or MinionDiveBomb or MinionSacrifice;
	}
}
