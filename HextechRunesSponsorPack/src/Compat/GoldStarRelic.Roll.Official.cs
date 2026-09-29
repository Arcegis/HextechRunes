#if STS2_108_OR_NEWER
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;

namespace HextechRunesSponsorPack;

// 0.108.0+:PotionRewardOdds.Roll(player, RoomType)。
public sealed partial class GoldStarRelic
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool RollPotionRewardCore(Player owner)
	{
		return owner.PlayerOdds.PotionReward.Roll(owner, RoomType.Monster);
	}
}
#endif
