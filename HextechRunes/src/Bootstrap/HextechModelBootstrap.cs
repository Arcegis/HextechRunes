namespace HextechRunes;

internal static class HextechModelBootstrap
{
	private static readonly object InstallLock = new();
	private static bool _installed;

	public static void Install()
	{
		lock (InstallLock)
		{
			if (_installed)
			{
				HextechLog.Info("Bootstrap", $"Model bootstrap already installed; skipping duplicate registration.");
				return;
			}

			HextechSavedPropertyBootstrap.InjectCaches();
			HextechModelPoolRegistrar.RegisterModels();
			_installed = true;
		}
	}

	// 仅为 HextechMobileModelRegistrationHooks 保留的转发;调用方改为直接调用 HextechModelPoolRegistrar 后即可删除。
	internal static void CleanupMobileFirstModelRegistrationWorkaround()
	{
		HextechModelPoolRegistrar.CleanupMobileFirstModelRegistrationWorkaround();
	}
}
