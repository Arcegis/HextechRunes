using MegaCrit.Sts2.Core.Localization;

namespace HextechRunes;

// 来源标签、海克斯标签和配置分组标题的键可能由外部模组提供。原版 LocTable 缺键直接抛 LocException，
// 会让整个选择界面或配置菜单构建失败，所以这里先查键，缺失时退回显示键名本身。
internal static class HextechRuneLabels
{
	/// <summary>本模组 UI 文案所在的原版 loc 表(模组的 relic_collection.json 合并进去)。</summary>
	internal const string LocTable = "relic_collection";

	internal static string GetPoolText(string poolKey)
	{
		return GetRawTextOrFallback("HEXTECH_POOL." + poolKey, poolKey);
	}

	internal static string GetTagText(string tagKey)
	{
		return GetRawTextOrFallback("HEXTECH_TAG." + tagKey, tagKey);
	}

	internal static string? TryGetText(string key)
	{
		return LocString.Exists(LocTable, key) ? new LocString(LocTable, key).GetRawText() : null;
	}

	private static string GetRawTextOrFallback(string key, string fallback)
	{
		return TryGetText(key) ?? fallback;
	}
}
