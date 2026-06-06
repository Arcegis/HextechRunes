using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechModelIdSerializationWarningHooks
{
	private const string Prefix = "Two AbstractModels ";
	private static readonly string Suffix = $" from mod {ModInfo.Id} share an ID! This might break multiplayer.";
	private static bool _installed;
	private static bool _loggedSuppression;

	public static void Install(Harmony harmony)
	{
		if (_installed)
		{
			return;
		}

		harmony.Patch(
			RequireMethod(typeof(Log), nameof(Log.Warn), BindingFlags.Static | BindingFlags.Public, typeof(string), typeof(int)),
			prefix: new HarmonyMethod(typeof(HextechModelIdSerializationWarningHooks), nameof(LogWarnPrefix)));
		_installed = true;
	}

	private static bool LogWarnPrefix(string text)
	{
		if (!IsFalsePositiveSelfCompareWarning(text, out string? typeFullName))
		{
			return true;
		}

		if (HasActualDuplicateModelType(typeFullName!))
		{
			return true;
		}

		if (!_loggedSuppression)
		{
			_loggedSuppression = true;
			Log.Info($"[{ModInfo.Id}][MultiplayerCompat] Suppressed false-positive ModelIdSerializationCache self-compare warning for {typeFullName}.");
		}

		return false;
	}

	private static bool IsFalsePositiveSelfCompareWarning(string? text, out string? typeFullName)
	{
		typeFullName = null;
		if (string.IsNullOrEmpty(text)
			|| !text.StartsWith(Prefix, StringComparison.Ordinal)
			|| !text.EndsWith(Suffix, StringComparison.Ordinal))
		{
			return false;
		}

		string body = text.Substring(Prefix.Length, text.Length - Prefix.Length - Suffix.Length);
		string[] parts = body.Split(" and ", StringSplitOptions.None);
		if (parts.Length != 2 || !string.Equals(parts[0], parts[1], StringComparison.Ordinal))
		{
			return false;
		}

		typeFullName = parts[0];
		return !string.IsNullOrWhiteSpace(typeFullName);
	}

	private static bool HasActualDuplicateModelType(string typeFullName)
	{
		int count = 0;
		try
		{
			foreach (Mod mod in ModManager.Mods)
			{
				if (mod.state != ModLoadState.Loaded
					|| !string.Equals(mod.manifest?.id, ModInfo.Id, StringComparison.Ordinal)
					|| mod.assembly == null)
				{
					continue;
				}

				foreach (Type type in mod.assembly.GetTypes())
				{
					if (!type.IsAbstract
						&& !type.IsInterface
						&& type.IsSubclassOf(typeof(AbstractModel))
						&& string.Equals(type.FullName, typeFullName, StringComparison.Ordinal)
						&& ++count > 1)
					{
						return true;
					}
				}
			}
		}
		catch
		{
			return true;
		}

		return false;
	}
}
