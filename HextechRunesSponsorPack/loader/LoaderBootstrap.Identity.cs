namespace HextechRunesSponsorPack.Loader;

// 拓展包加载器的身份常量。加载逻辑直接编译本体的 HextechRunes/loader/LoaderBootstrap.cs 与
// LinuxNativeDependencyBootstrap.cs(见 csproj),两边只在这三个常量上不同。
public static partial class LoaderBootstrap
{
	internal const string ModId = "HextechRunesSponsorPack";
	internal const string VariantManifestName = "hextech-runes-sponsor-pack-variants.manifest";
	internal const string CompatTargetMetadataKey = "HextechSponsorCompatibilityTarget";
}
