using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static class HextechAiTeammateCompat
{
	private const string AiLoopbackHostGameServiceTypeName = "AITeammate.Scripts.AiTeammateLoopbackHostGameService";
	private const string AiSessionRegistryTypeName = "AITeammate.Scripts.AiTeammateSessionRegistry";

	private static readonly Lazy<Type?> AiSessionRegistryType = new(ResolveAiSessionRegistryType);
	private static readonly Lazy<MethodInfo?> CanUseDirectSelectionAutomationMethod = new(() => GetRegistryMethod("CanUseDirectSelectionAutomation"));
	private static readonly Lazy<MethodInfo?> ShouldAutomateAiPlayerMethod = new(() => GetRegistryMethod("ShouldAutomateAiPlayer"));
	private static bool _warnedReflectionFailure;

	public static bool ShouldAutoSelectRune(Player? player)
	{
		if (player == null || RunManager.Instance?.NetService?.GetType().FullName != AiLoopbackHostGameServiceTypeName)
		{
			return false;
		}

		try
		{
			return InvokeRegistryBool(CanUseDirectSelectionAutomationMethod.Value, player)
				|| InvokeRegistryBool(ShouldAutomateAiPlayerMethod.Value, player);
		}
		catch (Exception ex)
		{
			if (!_warnedReflectionFailure)
			{
				_warnedReflectionFailure = true;
				Log.Warn($"[{ModInfo.Id}][Mayhem][AITeammateCompat] Failed to query AI teammate session registry: {ex.Message}");
			}

			return false;
		}
	}

	public static int PickRandomRuneIndex(Player player, IReadOnlyList<RelicModel> options)
	{
		if (options.Count == 0)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem][AITeammateCompat] No rune options for AI player={player.NetId}");
			return -1;
		}

		int selectedIndex = Random.Shared.Next(options.Count);
		RelicModel selectedRelic = options[selectedIndex];
		Log.Info($"[{ModInfo.Id}][Mayhem][AITeammateCompat] Auto-selected rune for AI player={player.NetId} index={selectedIndex} relic={(selectedRelic.CanonicalInstance?.Id ?? selectedRelic.Id).Entry}");
		return selectedIndex;
	}

	private static MethodInfo? GetRegistryMethod(string methodName)
	{
		return AiSessionRegistryType.Value?.GetMethod(
			methodName,
			BindingFlags.Public | BindingFlags.Static,
			null,
			[ typeof(Player) ],
			null);
	}

	private static bool InvokeRegistryBool(MethodInfo? method, Player player)
	{
		return method?.Invoke(null, [ player ]) is true;
	}

	private static Type? ResolveAiSessionRegistryType()
	{
		Type? type = Type.GetType($"{AiSessionRegistryTypeName}, sts2AITeammate", throwOnError: false);
		if (type != null)
		{
			return type;
		}

		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			type = assembly.GetType(AiSessionRegistryTypeName, throwOnError: false);
			if (type != null)
			{
				return type;
			}
		}

		return null;
	}
}
