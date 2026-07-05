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
				HextechLog.Info($"[{ModInfo.Id}] Model bootstrap already installed; skipping duplicate registration.");
				return;
			}

			HextechSavedPropertyBootstrap.InjectCaches();
			HextechModelPoolRegistrar.RegisterModels();
			_installed = true;
		}
	}

	internal static void CleanupMobileFirstModelRegistrationWorkaround()
	{
		HextechModelPoolRegistrar.CleanupMobileFirstModelRegistrationWorkaround();
	}
}
