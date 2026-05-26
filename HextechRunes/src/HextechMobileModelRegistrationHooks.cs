using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechMobileModelRegistrationHooks
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(ModelDb), nameof(ModelDb.Init), BindingFlags.Static | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechMobileModelRegistrationHooks), nameof(ModelDbInitPostfix)));
	}

	private static void ModelDbInitPostfix()
	{
		try
		{
			HextechModelBootstrap.CleanupMobileFirstModelRegistrationWorkaround();
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}] Android model registration workaround cleanup skipped: {ex.GetType().Name}: {ex.Message}");
		}
	}
}
