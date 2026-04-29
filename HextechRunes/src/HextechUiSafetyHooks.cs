using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MonoMod.RuntimeDetour;

namespace HextechRunes;

internal static class HextechUiSafetyHooks
{
	private static Hook? _relicAcquiredAnimationHook;
	private static int _relicAnimationSkipLogs;

	private delegate Task OrigPlayNewlyAcquiredAnimation(NRelicInventoryHolder self, Vector2? startLocation, Vector2? startScale);

	public static void Install()
	{
		_relicAcquiredAnimationHook = new Hook(
			RequireMethod(typeof(NRelicInventoryHolder), nameof(NRelicInventoryHolder.PlayNewlyAcquiredAnimation), BindingFlags.Instance | BindingFlags.Public, typeof(Vector2?), typeof(Vector2?)),
			PlayNewlyAcquiredAnimationDetour);
	}

	private static async Task PlayNewlyAcquiredAnimationDetour(OrigPlayNewlyAcquiredAnimation orig, NRelicInventoryHolder self, Vector2? startLocation, Vector2? startScale)
	{
		if (!IsNodeUsable(self))
		{
			LogRelicAnimationSkipped("holder-not-in-tree");
			return;
		}

		try
		{
			await orig(self, startLocation, startScale);
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

	private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] parameters)
	{
		return type.GetMethod(name, flags, binder: null, parameters, modifiers: null)
			?? throw new InvalidOperationException($"Could not find required method {type.FullName}.{name}.");
	}
}
