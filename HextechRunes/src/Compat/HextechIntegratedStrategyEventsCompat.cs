namespace HextechRunes;

internal static class HextechIntegratedStrategyEventsCompat
{
	private const string AssemblyName = "IntegratedStrategyEvents";
	private const string InteropTypeName = "IntegratedStrategyEvents.IntegratedStrategyEventsInterop";
	private const string MethodName = "GetCurrentExtraActId";
	private static MethodInfo? _getCurrentExtraActId;

	public static void Install()
	{
		HextechRunesInterop.RegisterExtraActProvider(GetCurrentExtraActId);
	}

	private static string? GetCurrentExtraActId(IRunState runState)
	{
		MethodInfo? method = _getCurrentExtraActId ??= ResolveMethod();
		if (method == null)
		{
			return null;
		}

		try
		{
			return method.Invoke(null, [ runState ]) as string;
		}
		catch (Exception ex)
		{
			if (HextechRunLogBudget.TryConsume("compat.integrated-strategy-extra-act", 1))
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem] Integrated Strategy extra-act query failed: {ex.Message}");
			}
			return null;
		}
	}

	private static MethodInfo? ResolveMethod()
	{
		Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
			.FirstOrDefault(static candidate =>
				string.Equals(candidate.GetName().Name, AssemblyName, StringComparison.Ordinal));
		Type? interopType = assembly?.GetType(InteropTypeName, throwOnError: false);
		return interopType?.GetMethod(
			MethodName,
			BindingFlags.Public | BindingFlags.Static,
			binder: null,
			types: [ typeof(IRunState) ],
			modifiers: null);
	}
}
