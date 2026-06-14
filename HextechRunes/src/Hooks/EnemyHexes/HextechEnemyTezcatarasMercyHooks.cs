using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechEnemyTezcatarasMercyHooks
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(RelicCmd), nameof(RelicCmd.Obtain), BindingFlags.Public | BindingFlags.Static, typeof(RelicModel), typeof(Player), typeof(int)),
			prefix: new HarmonyMethod(typeof(HextechEnemyTezcatarasMercyHooks), nameof(ObtainPrefix)));
	}

	private static void ObtainPrefix(RelicModel relic, Player player)
	{
		if (TezcatarasMercyEnemyHex.ShouldConvertRelic(player, relic))
		{
			relic.IsWax = true;
		}
	}
}
