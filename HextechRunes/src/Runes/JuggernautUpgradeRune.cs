using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

public sealed class JuggernautUpgradeRune : CardUpgradeRuneBase<Juggernaut>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsIroncladPlayer(player);
	}
}
