namespace HextechRunes.Loader;

// 本体加载器的身份常量。共享的 LoaderBootstrap.cs 只从这里读 mod 身份;拓展包 loader 有自己的一份。
public static partial class LoaderBootstrap
{
	internal const string ModId = "HextechRunes";
	internal const string VariantManifestName = "hextech-runes-variants.manifest";
	internal const string CompatTargetMetadataKey = "HextechCompatibilityTarget";
}
