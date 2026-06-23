using System.Reflection;
using HarmonyLib;
using HextechRunes;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;

namespace HextechRunesSponsorPack;

internal static class IntegratedStrategyEventsCompatibilityHooks
{
	private const string HarmonyId = "Natsuki.HextechRunesSponsorPack.IntegratedStrategyEventsCompat";
	private static readonly object InstallLock = new();
	private static Harmony? _harmony;
	private static bool _installed;

	internal static void Install()
	{
		lock (InstallLock)
		{
			if (_installed)
			{
				return;
			}

			Harmony harmony = _harmony ??= new Harmony(HarmonyId);
			harmony.Patch(
				RequireMethod(typeof(Creature), nameof(Creature.SetCurrentHpInternal), BindingFlags.Instance | BindingFlags.Public, typeof(decimal)),
				prefix: new HarmonyMethod(typeof(IntegratedStrategyEventsCompatibilityHooks), nameof(SetCurrentHpInternalPrefix)));
			_installed = true;
			Log.Info($"[{ModInfo.Id}] Installed IntegratedStrategyEvents compatibility hooks.");
		}
	}

	private static void SetCurrentHpInternalPrefix(Creature __instance, decimal amount)
	{
		if (amount <= __instance.MaxHp || !IntegratedStrategyEventsBridge.IsFinalChorale(__instance))
		{
			return;
		}

		__instance.SetMaxHpInternal(amount);
	}

	private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] parameters)
	{
		return type.GetMethod(name, flags, binder: null, parameters, modifiers: null)
			?? throw new InvalidOperationException($"Could not find required method {type.FullName}.{name}.");
	}
}
