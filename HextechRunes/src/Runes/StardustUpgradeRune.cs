namespace HextechRunes;

public sealed class StardustUpgradeRune : CardUpgradeRuneBase<Stardust>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsRegentPlayer(player);
	}

	internal static bool ShouldPreserveStars(CardModel card)
	{
		return card is Stardust && card.Owner?.GetRelic<StardustUpgradeRune>() != null;
	}
}
