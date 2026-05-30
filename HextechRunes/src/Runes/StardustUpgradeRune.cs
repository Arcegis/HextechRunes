using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

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
