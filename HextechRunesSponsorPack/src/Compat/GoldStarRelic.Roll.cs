using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs; // 只有 STS2_107_1 分支用(RunManager.Instance),0.108+ 目标下 IDE0005 会报它多余,不要删

namespace HextechRunesSponsorPack;

// 0.108.0 起 PotionRewardOdds.Roll 去掉了 AscensionManager 参数;版本差异收在这个分部文件里,GoldStarRelic 主体不带 #if。
public sealed partial class GoldStarRelic
{
	private static bool RollPotionReward(Player owner)
	{
#if STS2_107_1
		return owner.PlayerOdds.PotionReward.Roll(owner, RunManager.Instance!.AscensionManager, RoomType.Monster);
#else
		return owner.PlayerOdds.PotionReward.Roll(owner, RoomType.Monster);
#endif
	}
}
