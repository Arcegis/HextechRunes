using MegaCrit.Sts2.Core.Entities.Players;

namespace HextechRunes;

public sealed class DeviantCognitionRune : HextechRelicBase
{
	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}
}
