#if STS2_107_1
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunesSponsorPack;

// 0.107.1:PotionRewardOdds.Roll(player, AscensionManager, RoomType)。调用方已确认 AscensionManager 存在。
public sealed partial class GoldStarRelic
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool RollPotionRewardCore(Player owner)
	{
		AscensionManager? ascension = RunManager.Instance?.AscensionManager;
		return ascension != null && owner.PlayerOdds.PotionReward.Roll(owner, ascension, RoomType.Monster);
	}
}
#endif
