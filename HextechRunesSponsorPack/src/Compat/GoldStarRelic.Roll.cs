using MegaCrit.Sts2.Core.Entities.Players;

namespace HextechRunesSponsorPack;

// 0.108.0 起 PotionRewardOdds.Roll 去掉了 AscensionManager 参数;两种签名的调用分别在
// GoldStarRelic.Roll.Legacy.cs(STS2_107_1)与 GoldStarRelic.Roll.Official.cs(STS2_108_OR_NEWER)整文件隔离,
// GoldStarRelic 主体与本文件都不带 #if。
//
// 加载器按"不高于宿主的最大打包目标"选变体:宿主是 0.108.x / 0.109.x 时会拿到 0.107.1 变体,
// 这时三参 Roll 不存在,JIT 编译 RollPotionRewardCore 会抛 MissingMethodException。把真正的调用
// 隔离在一个禁止内联的方法里,调用方就能接住这个异常:金星在那两个版本上只是不掉药水,不会卡死精英结算。
public sealed partial class GoldStarRelic
{
	private static bool _loggedRollSignatureMismatch;

	private static bool RollPotionReward(Player owner)
	{
		try
		{
			return RollPotionRewardCore(owner);
		}
		catch (MissingMethodException ex)
		{
			if (!_loggedRollSignatureMismatch)
			{
				_loggedRollSignatureMismatch = true;
				SponsorLog.Warn("GoldStar", $"Potion roll unavailable on this game version (variant compiled for {ModInfo.TargetGameVersion}): {ex.Message}");
			}

			return false;
		}
	}
}
