using System.Reflection;

namespace HextechRunesSponsorPack;

internal static class ModInfo
{
	public const string Id = "HextechRunesSponsorPack";

	public const string DisplayName = "海克斯大乱斗：赞助者拓展包";

	// 唯一版本来源是 csproj 的 <Version>;手写常量会与 manifest / csproj 漂移。
	public static string Version { get; } =
		typeof(ModInfo).Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
			?.Split('+')[0]
		?? typeof(ModInfo).Assembly.GetName().Version?.ToString(3)
		?? "0.0.0";
}
