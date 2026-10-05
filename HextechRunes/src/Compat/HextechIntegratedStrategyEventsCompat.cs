namespace HextechRunes;

internal static class HextechIntegratedStrategyEventsCompat
{
	private const string AssemblyName = "IntegratedStrategyEvents";
	private const string InteropTypeName = "IntegratedStrategyEvents.IntegratedStrategyEventsInterop";
	private const string MethodName = "GetCurrentExtraActId";

	private static readonly HextechLoadedAssemblyLookup IntegratedStrategyAssembly = new(AssemblyName, StringComparison.Ordinal);

	// 找到程序集后只解析一次方法（包括"程序集在但方法缺失"的结果）；没装 ISE 时由上面的查找缓存负责。
	private static MethodInfo? _getCurrentExtraActId;
	private static bool _extraActMethodResolved;

	public static void Install()
	{
		HextechRunesInterop.RegisterExtraActProvider(GetCurrentExtraActId);
	}

	private static string? GetCurrentExtraActId(IRunState runState)
	{
		if (IntegratedStrategyAssembly.Find() is not Assembly assembly)
		{
			return null;
		}

		if (!_extraActMethodResolved)
		{
			_getCurrentExtraActId = ResolveMethod(assembly);
			_extraActMethodResolved = true;
		}

		// 异常不在这里接:HextechRunesInterop.GetCurrentExtraActId 对每个提供者都有 catch 并限次告警。
		// DoNotWrapExceptions 让那里的告警拿到 ISE 自己的异常信息,而不是 TargetInvocationException。
		return _getCurrentExtraActId?.Invoke(null, BindingFlags.DoNotWrapExceptions, binder: null, [ runState ], culture: null) as string;
	}

	private static MethodInfo? ResolveMethod(Assembly assembly)
	{
		Type? interopType = assembly.GetType(InteropTypeName, throwOnError: false);
		MethodInfo? method = interopType?.GetMethod(
			MethodName,
			BindingFlags.Public | BindingFlags.Static,
			binder: null,
			types: [ typeof(IRunState) ],
			modifiers: null);
		if (method == null && HextechRunLogBudget.TryConsume("compat.integrated-strategy-extra-act-api-missing", 1))
		{
			HextechLog.Warn("Mayhem", $"Integrated Strategy is loaded but does not expose {InteropTypeName}.{MethodName}(IRunState); finale acts cannot trigger Hextech acquisition until Integrated Strategy is updated.");
		}

		return method;
	}
}
