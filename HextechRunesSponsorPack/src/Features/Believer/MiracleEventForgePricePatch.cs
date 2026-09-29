using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunesSponsorPack;

// 本体售价助手是 internal，因此按完整类型名和签名反射安装 postfix，叠加信徒的本局价格修正。
// 目标缺失时抛出，由 SponsorPatcher 按 Optional 记录 Info，并纳入启动失败摘要。
[SponsorPatch("believer.forge-price", "信徒·锻造器售价修正", Optional = true)]
internal static class MiracleEventForgePricePatch
{
	internal static void Apply(Harmony harmony)
	{
		Type? helper = AccessTools.TypeByName("HextechRunes.HextechForgeShopPriceHelper");
		MethodInfo? target = helper == null
			? null
			: AccessTools.Method(helper, "GetRandomForgeShopPriceFor", [ typeof(RunState) ]);
		if (target == null)
		{
			throw new MissingMethodException("HextechRunes.HextechForgeShopPriceHelper", "GetRandomForgeShopPriceFor");
		}

		harmony.Patch(target, postfix: new HarmonyMethod(typeof(MiracleEventForgePricePatch), nameof(Postfix)));
		Log.Info($"[{ModInfo.Id}] Miracle forge-price patch installed on {target.DeclaringType?.Name}.{target.Name}.");
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
