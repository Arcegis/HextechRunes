using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
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
			HextechLog.Info($"[{ModInfo.Id}][MultiplayerCompat] Suppressed false-positive ModelIdSerializationCache self-compare warning for {typeFullName}.");
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
					|| !string.Equals(mod.manifest?.id, ModInfo.Id, StringComparison.Ordinal))
				{
					continue;
				}

#if STS2_108_OR_NEWER
				// 0.108.0 起 Mod 支持多程序集:单 assembly 字段改为 assemblies 列表。
				System.Reflection.Assembly? modAssembly = mod.assemblies.FirstOrDefault();
#else
				System.Reflection.Assembly? modAssembly = mod.assembly;
#endif
				if (modAssembly == null)
				{
					continue;
				}

				foreach (Type type in modAssembly.GetTypes())
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
