namespace HextechRunes;

internal static class HextechRuntimeRuneCompatibility
{
	private static readonly HashSet<Type> HookFailedPlayerRuneTypes = [];

	public static bool IsAndroidRuntime
	{
		get
		{
			try
			{
				return OperatingSystem.IsAndroid();
			}
			catch
			{
				return false;
			}
		}
	}

	public static void MarkPlayerRuneHookFailed<TRune>(string label, Exception exception)
		where TRune : RelicModel
	{
		Type runeType = typeof(TRune);
		bool firstFailure = HookFailedPlayerRuneTypes.Add(runeType);
		string state = firstFailure ? "disabled" : "already disabled";
		Log.Warn($"[{ModInfo.Id}][Mayhem][Compat] Player rune hook failed; {state} for this runtime: rune={runeType.Name} hook={label} error={exception.GetType().Name}: {exception.Message}");
	}

	public static bool IsPlayerRuneAvailableForCurrentRuntime(Type runeType)
	{
		return !HookFailedPlayerRuneTypes.Contains(runeType);
	}
}
