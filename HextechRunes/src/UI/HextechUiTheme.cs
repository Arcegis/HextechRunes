using Godot;
using MegaCrit.Sts2.addons.mega_text;

namespace HextechRunes;

/// <summary>本模组自建界面(配置菜单、社区面板等)的共用配色与主题辅助;只收在多处使用的颜色。</summary>
internal static class HextechUiTheme
{
	// 按钮(动作按钮与步进按钮共用四态)。
	internal static readonly Color ButtonBackground = new(0.1f, 0.12f, 0.17f, 0.9f);
	internal static readonly Color ButtonBackgroundHover = new(0.13f, 0.16f, 0.22f, 0.95f);
	internal static readonly Color ButtonBackgroundPressed = new(0.07f, 0.09f, 0.13f, 0.98f);
	internal static readonly Color ButtonBorder = new(0.46f, 0.55f, 0.68f, 0.78f);
	internal static readonly Color ButtonBorderHighlight = new(0.88f, 0.72f, 0.36f, 0.92f);
	internal static readonly Color ButtonBorderPressed = new(0.88f, 0.62f, 0.28f, 0.92f);
	internal static readonly Color ButtonText = new(0.96f, 0.94f, 0.88f, 1f);

	/// <summary>较淡的钢蓝描边:开关轨道关闭态、社区配置卡片。</summary>
	internal static readonly Color SoftSteelBorder = new(0.46f, 0.55f, 0.68f, 0.5f);

	/// <summary>金色细分隔线。</summary>
	internal static readonly Color Hairline = new(0.86f, 0.74f, 0.42f, 0.28f);

	// 文字。
	internal static readonly Color DialogTitleText = new(0.95f, 0.87f, 0.62f, 1f);
	internal static readonly Color SectionDescriptionText = new(0.78f, 0.84f, 0.9f, 0.9f);
	internal static readonly Color OptionDescriptionText = new(0.78f, 0.84f, 0.9f, 0.88f);
	internal static readonly Color HintText = new(0.78f, 0.82f, 0.9f, 0.85f);
	internal static readonly Color StepperLabelText = new(0.92f, 0.9f, 0.78f, 0.96f);
	internal static readonly Color NumberText = new(0.98f, 0.98f, 0.94f, 1f);

	/// <summary>全透明(淡入动画起点的 modulate)。</summary>
	internal static readonly Color TransparentWhite = new(1f, 1f, 1f, 0f);

	/// <summary>全透明背景(只画描边的 StyleBox、未激活的页签)。</summary>
	internal static readonly Color TransparentBlack = new(0f, 0f, 0f, 0f);

	internal static void ApplyDefaultMegaLabelTheme(MegaLabel label)
	{
		Font font = label.GetThemeDefaultFont();
		if (font != null)
		{
			label.AddThemeFontOverride("font", font);
		}

		int fontSize = label.GetThemeDefaultFontSize();
		if (fontSize > 0)
		{
			label.AddThemeFontSizeOverride("font_size", fontSize);
		}
	}
}
