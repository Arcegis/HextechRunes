using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

public static class RelicBundleGrantHelper
{
	public static async Task GrantRelics(Player player, IEnumerable<Type> relicTypes)
	{
		foreach (Type type in relicTypes)
		{
			RelicModel relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(type)).ToMutable();

			// 先古遗物(古老牙齿/欧洛巴斯之触)依赖玩家牌组里有原版起手牌、或有起手稀有度遗物才能正确初始化。
			// 对不满足前提的角色(典型:其它角色 mod),原版 SetupForPlayer 返回 false;此时若强行 Obtain,
			// 古老牙齿 AfterObtained 会因 GetTranscendenceStarterCard=null 解引用抛 NRE —— 既让该遗物"不生效",
			// 又中断同一捆包里后续遗物的授予(连欧洛巴斯之触也拿不到)。改为:SetupForPlayer 不通过就确定性跳过该件。
			// 判定只读已同步的 player.Deck/Relics,各端结论一致,不引入联机分叉;不改任何 SavedProperty/序列化。
			if (!CanGrantBundledRelic(relic, player))
			{
				HextechLog.Info($"[{ModInfo.Id}] RelicBundleGrant: skipped {relic.Id.Entry} for player {player.NetId} (not applicable to this character)");
				continue;
			}

			SaveManager.Instance.MarkRelicAsSeen(relic);
			await RelicCmd.Obtain(relic, player);
		}
	}

	// 古老牙齿/欧洛巴斯之触用游戏自带 SetupForPlayer 判定能否对该角色生效(确定性、只读牌组/遗物);
	// 其余遗物无此前提,一律可授予。SetupForPlayer 返回 true 时同步预置好其字段,后续 AfterObtained 重算也必非 null。
	private static bool CanGrantBundledRelic(RelicModel relic, Player player)
	{
		return relic switch
		{
			ArchaicTooth tooth => tooth.SetupForPlayer(player),
			TouchOfOrobas touch => touch.SetupForPlayer(player),
			_ => true,
		};
	}
}
