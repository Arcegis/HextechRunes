using MegaCrit.Sts2.Core.Entities.Players;

namespace HextechRunes;

public sealed class ElectrodynamicsRune : HextechRelicBase
{
	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}
}
