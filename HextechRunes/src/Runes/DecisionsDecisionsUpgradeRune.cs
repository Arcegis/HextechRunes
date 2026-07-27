namespace HextechRunes;

public sealed class DecisionsDecisionsUpgradeRune : CardUpgradeRuneBase<DecisionsDecisions>
{
	protected override bool IsAvailableForCharacter(Player player) => IsRegentPlayer(player);

	internal static bool CanSelectCard(CardModel card)
	{
		return CanSelectCard(card.Keywords.Contains(CardKeyword.Unplayable));
	}

	internal static bool CanSelectCard(bool isUnplayable) => !isUnplayable;
}
