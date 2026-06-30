using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;
using SponsorModInfo = HextechRunesSponsorPack.ModInfo;

namespace HextechRunes;

// 神迹事件的「锻造器售价」本局临时修正:主 mod 的 HextechForgeShopPriceHelper.GetRandomForgeShopPriceFor
// 是 internal,拓展包用 Harmony(反射定位)postfix,把本局 BelieverRune 累计的售价修正叠加到算出的价格上。
// 纯拓展包,主 mod 源码一行不动。
internal static class MiracleEventForgePricePatch
{
	private const string HarmonyId = "Natsuki.HextechRunesSponsorPack.MiracleForgePrice";

	private static Harmony? _harmony;

	internal static void Install()
	{
		try
		{
			Type? helper = AccessTools.TypeByName("HextechRunes.HextechForgeShopPriceHelper");
			MethodInfo? target = helper == null
				? null
				: AccessTools.Method(helper, "GetRandomForgeShopPriceFor", [ typeof(RunState) ]);
			if (target == null)
			{
				Log.Warn($"[{SponsorModInfo.Id}] Miracle forge-price patch skipped: GetRandomForgeShopPriceFor not found.", 2);
				return;
			}

			Harmony harmony = _harmony ??= new Harmony(HarmonyId);
			harmony.Patch(target, postfix: new HarmonyMethod(typeof(MiracleEventForgePricePatch), nameof(Postfix)));
			Log.Info($"[{SponsorModInfo.Id}] Miracle forge-price patch installed on {target.DeclaringType?.Name}.{target.Name}.");
		}
		catch (Exception ex)
		{
			Log.Warn($"[{SponsorModInfo.Id}] Miracle forge-price patch failed: {ex.GetType().Name}: {ex.Message}", 2);
		}
	}

	private static void Postfix(RunState runState, ref int __result)
	{
		// 商店算价(ModifyMerchantPrice)可能传 null 的 runState(shopRelic.Owner 为 null) —— 此时从 RunManager 兜底取本局,
		// 否则会漏掉售价修正、显示成基础价。
		RunState? state = runState ?? GetActiveRunState();
		if (state == null)
		{
			return;
		}

		int delta = state.Players
			.SelectMany(player => player.Relics)
			.OfType<BelieverRune>()
			.Sum(believer => believer.ForgePriceDelta);
		if (delta != 0)
		{
			__result = Math.Max(0, __result + delta);
		}
	}

	private static RunState? GetActiveRunState()
	{
		try
		{
			return RunManager.Instance?.DebugOnlyGetState() as RunState;
		}
		catch
		{
			return null;
		}
	}
}
