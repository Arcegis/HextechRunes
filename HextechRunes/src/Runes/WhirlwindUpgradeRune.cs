namespace HextechRunes;

public sealed class WhirlwindUpgradeRune : CardUpgradeRuneBase<Whirlwind>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsIroncladPlayer(player);
	}

	internal static void TryDoubleResolvedX(CardModel card, ref int xValue)
	{
		if (xValue < 3
			|| card is not Whirlwind
			|| card.Owner?.GetRelic<WhirlwindUpgradeRune>() == null)
		{
			return;
		}

		xValue *= 2;
	}
}
