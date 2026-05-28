using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Relics;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechUiSafetyHooks
{
	private static int _relicAnimationSkipLogs;

	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(NRelicInventoryHolder), nameof(NRelicInventoryHolder.PlayNewlyAcquiredAnimation), BindingFlags.Instance | BindingFlags.Public, typeof(Vector2?), typeof(Vector2?)),
			prefix: new HarmonyMethod(typeof(HextechUiSafetyHooks), nameof(PlayNewlyAcquiredAnimationPrefix)),
			postfix: new HarmonyMethod(typeof(HextechUiSafetyHooks), nameof(PlayNewlyAcquiredAnimationPostfix)));
	}

	private static bool PlayNewlyAcquiredAnimationPrefix(NRelicInventoryHolder __instance, ref Task __result, out bool __state)
	{
		__state = false;
		if (!IsNodeUsable(__instance))
		{
			LogRelicAnimationSkipped("holder-not-in-tree");
			__result = Task.CompletedTask;
			return false;
		}

		__state = true;
		return true;
	}

	private static void PlayNewlyAcquiredAnimationPostfix(NRelicInventoryHolder __instance, bool __state, ref Task __result)
	{
		if (__state)
		{
			__result = PlayNewlyAcquiredAnimationSafely(__result, __instance);
		}
	}

	private static async Task PlayNewlyAcquiredAnimationSafely(Task original, NRelicInventoryHolder self)
	{
		try
		{
			await original;
		}
		catch (NullReferenceException) when (!IsNodeUsable(self))
		{
			LogRelicAnimationSkipped("holder-left-tree");
		}
		catch (ObjectDisposedException) when (!GodotObject.IsInstanceValid(self))
		{
			LogRelicAnimationSkipped("holder-disposed");
		}
	}

	private static bool IsNodeUsable(Node node)
	{
		return GodotObject.IsInstanceValid(node) && node.IsInsideTree();
	}

	private static void LogRelicAnimationSkipped(string reason)
	{
		if (_relicAnimationSkipLogs++ < 5)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Relic acquired animation skipped: {reason}");
		}
	}

}
