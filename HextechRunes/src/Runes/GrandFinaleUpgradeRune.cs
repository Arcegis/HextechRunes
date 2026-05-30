using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

public sealed class GrandFinaleUpgradeRune : CardUpgradeRuneBase<GrandFinale>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsSilentPlayer(player);
	}

	internal static bool AllowsPlaying(CardModel card)
	{
		return card is GrandFinale && card.Owner?.GetRelic<GrandFinaleUpgradeRune>() != null;
	}
}
