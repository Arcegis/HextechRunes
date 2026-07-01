using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using SponsorModInfo = HextechRunesSponsorPack.ModInfo;

namespace HextechRunes;

// 「神迹」事件的安全注入点(信徒海克斯)。信徒不在战斗胜利 hook 里直接 EnterRoom —— 那会把刚打赢、奖励还在的战斗房
// 连同流程一起弹掉(EnterRoom 内部先 ExitCurrentRooms),造成「打完 Boss 不自动进下一层要 SL / SL 时事件丢失 / 末战无法结算」。
//
// 改在玩家「战斗领奖屏点继续」时拦截 RunManager.ProceedFromTerminalRewardsScreen —— 此刻战斗房已领奖、流程空闲,是干净交接点。
// 单人 + 栈深 1 + 当前是战斗房 + 非末幕 Boss 且有待触发计数时:消耗一次计数,改为 EnterRoom(神迹)(干净弹掉已领奖的战斗房),
// 并 return false 跳过原方法(原本只是开图)。神迹完成后玩家点 PROCEED,由 NEventRoom.Proceed 独立开图(不重入本补丁)。
//
// SL 安全:计数是 SavedProperty,且在这一刻才于内存里消耗(晚于「战斗胜利后」那次存档)。所以 SL 回到事件中途,会落到
// 「计数仍 >0、战斗房 pre-finished」的存档 → 再次 proceed 时神迹重现,而非消失。末幕 Boss 守卫则避免在终局插事件破坏结算。
// 纯拓展包,主 mod 一行不动。
internal static class MiracleEventTriggerPatch
{
	private const string HarmonyId = "Natsuki.HextechRunesSponsorPack.MiracleTrigger";

	private static Harmony? _harmony;

	internal static void Install()
	{
		try
		{
			MethodInfo? target = AccessTools.Method(typeof(RunManager), nameof(RunManager.ProceedFromTerminalRewardsScreen));
			if (target == null)
			{
				Log.Warn($"[{SponsorModInfo.Id}] Miracle trigger patch skipped: ProceedFromTerminalRewardsScreen not found.", 2);
				return;
			}

			Harmony harmony = _harmony ??= new Harmony(HarmonyId);
			harmony.Patch(target, prefix: new HarmonyMethod(typeof(MiracleEventTriggerPatch), nameof(Prefix)));
			Log.Info($"[{SponsorModInfo.Id}] Miracle trigger patch installed on RunManager.ProceedFromTerminalRewardsScreen.");
		}
		catch (Exception ex)
		{
			Log.Warn($"[{SponsorModInfo.Id}] Miracle trigger patch failed: {ex.GetType().Name}: {ex.Message}", 2);
		}
	}

	// 返回 false 跳过原方法(原本只是开图);改为先进神迹,事件离开时由 NEventRoom.Proceed 开图。
	private static bool Prefix(RunManager __instance, ref Task __result)
	{
		try
		{
			if (HextechRelicBase.IsNetworkMultiplayerRun())
			{
				return true;
			}

			RunState? state = __instance.DebugOnlyGetState();
			if (state == null || state.CurrentRoomCount != 1)
			{
				return true;
			}

			// 只在「战斗领奖→开图」的交接点注入(事件领奖走 NEventRoom.Proceed,不经过这里)。
			if (state.CurrentRoom is not CombatRoom combatRoom)
			{
				return true;
			}

			// 末战守卫(bug:末战拿信徒无法结算):末幕 Boss 之后不插事件,留给 EnterNextAct → WinRun 正常结算。
			if (combatRoom.RoomType == RoomType.Boss && state.CurrentActIndex >= state.Acts.Count - 1)
			{
				return true;
			}

			BelieverRune? believer = state.Players
				.SelectMany(static player => player.Relics)
				.OfType<BelieverRune>()
				.FirstOrDefault(static relic => relic.HasPendingMiracle);
			if (believer == null)
			{
				return true;
			}

			believer.ConsumePendingMiracle();
			__result = EnterMiracle(__instance);
			return false;
		}
		catch (Exception ex)
		{
			Log.Warn($"[{SponsorModInfo.Id}] Miracle trigger prefix error: {ex.GetType().Name}: {ex.Message}", 2);
			return true;
		}
	}

	private static async Task EnterMiracle(RunManager runManager)
	{
		try
		{
			EventModel miracle = ModelDb.Event<MiracleEvent>();
			await runManager.EnterRoom(new EventRoom(miracle));
		}
		catch (Exception ex)
		{
			Log.Warn($"[{SponsorModInfo.Id}] BelieverRune failed to enter Miracle event: {ex.GetType().Name}: {ex.Message}", 2);
			// 兜底:进事件失败也要开图,避免玩家卡在领奖屏(跳过原方法后地图不会自动打开)。
			try
			{
				NMapScreen.Instance?.SetTravelEnabled(true);
				NMapScreen.Instance?.Open();
			}
			catch
			{
			}
		}
	}
}
