using System.Text.RegularExpressions;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;

namespace HextechRunes;

internal static partial class HextechCollectionHooks
{
	// 原版「初始」分类标题的富文本骨架:[gold][font_size=..][b]标题[/b][/font_size][/gold] 正文。
	// 只替换标题与正文,字号/加粗/颜色沿用原版当前语言的写法。
	private static readonly Regex StarterHeaderPattern = new(
		@"^(?<open>.*?\[b\])(?<title>.*?)(?<close>\[/b\].*?\[/gold\])(?<gap>\s*)(?<body>.*)$",
		RegexOptions.Singleline | RegexOptions.CultureInvariant);

	// 本模组 loc 表里分类标题的写法:[gold]标题[/gold] 正文。
	private static readonly Regex OwnHeaderPattern = new(
		@"^\s*\[gold\](?<title>.*?)\[/gold\]\s*(?<body>.*)$",
		RegexOptions.Singleline | RegexOptions.CultureInvariant);

	/// <summary>
	/// 子分类标题:取本模组 loc 表的标题与正文,套进原版「初始」标题的富文本骨架,与原版分类视觉一致(九种语言通用)。
	/// 骨架或文案格式对不上时直接用 loc 表原文。
	/// </summary>
	private static void ApplyCustomHeaderText(NRelicCollectionCategory subCategory, string localizationKey)
	{
		if (HeaderLabelField?.GetValue(subCategory) is not MegaRichTextLabel headerLabel
			|| HextechRuneLabels.TryGetText(localizationKey) is not { } ownText)
		{
			return;
		}

		headerLabel.SetTextAutoSize(FormatLikeStarterHeader(_starterHeaderTemplate, ownText));
	}

	private static string FormatLikeStarterHeader(string? starterTemplate, string ownText)
	{
		if (string.IsNullOrWhiteSpace(starterTemplate))
		{
			return ownText;
		}

		Match template = StarterHeaderPattern.Match(starterTemplate);
		Match own = OwnHeaderPattern.Match(ownText);
		if (!template.Success || !own.Success)
		{
			return ownText;
		}

		return template.Groups["open"].Value
			+ own.Groups["title"].Value
			+ template.Groups["close"].Value
			+ template.Groups["gap"].Value
			+ own.Groups["body"].Value;
	}

	private static bool CanUseSubcategoryHooks()
	{
		return HeaderLabelField != null
			&& SubCategoriesField != null
			&& CreateForSubcategoryMethod != null
			&& LoadSubcategoryMethod != null;
	}

	private static IEnumerable<string> GetMissingSubcategoryDependencies()
	{
		if (HeaderLabelField == null)
		{
			yield return "NRelicCollectionCategory._headerLabel";
		}

		if (SubCategoriesField == null)
		{
			yield return "NRelicCollectionCategory._subCategories";
		}

		if (CreateForSubcategoryMethod == null)
		{
			yield return "NRelicCollectionCategory.CreateForSubcategory";
		}

		if (LoadSubcategoryMethod == null)
		{
			yield return "NRelicCollectionCategory.LoadSubcategory";
		}
	}
}
