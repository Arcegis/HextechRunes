using System.Reflection;

namespace HextechRunesSponsorPack;

internal static class ModInfo
{
	public const string Id = "HextechRunesSponsorPack";

	// 本变体编译时对准的游戏版本,来自 csproj 的 AssemblyMetadata(加载器也按这个键选变体)。
	public static string TargetGameVersion { get; } =
		typeof(ModInfo).Assembly
			.GetCustomAttributes<AssemblyMetadataAttribute>()
			.FirstOrDefault(static attribute => attribute.Key == "HextechSponsorCompatibilityTarget")
			?.Value
		?? "unknown";
}
