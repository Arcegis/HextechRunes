using Godot;

namespace HextechRunes;

// 配置菜单的 StyleBox、稀有度配色与响应式尺寸。
internal static partial class HextechRuneConfigMenuHooks
{
	private static Vector2 GetResponsivePanelSize()
	{
		Vector2I windowSize = DisplayServer.WindowGetSize();
		float windowWidth = windowSize.X > 0 ? windowSize.X : 1280f;
		float windowHeight = windowSize.Y > 0 ? windowSize.Y : 720f;
		bool compactLayout = windowHeight < CompactConfigHeightThreshold;
		// 面板必须始终容得下符文网格加自身边距,切页签时边框宽度才不变;超宽屏再设上限。
		float panelMargins = (compactLayout ? 20f : 28f) * 2f;
		float minWidth = GetRuneGridMinWidth(compactLayout) + panelMargins;
		float maxWidth = Math.Max(minWidth, 1080f);
		float width = windowWidth < minWidth
			? Math.Max(320f, windowWidth * 0.98f)
			: Mathf.Clamp(windowWidth * 0.9f, minWidth, maxWidth);
		float height = windowHeight < CompactConfigHeightThreshold
			? Math.Max(440f, windowHeight * 0.98f)
			: Mathf.Clamp(windowHeight * 0.92f, 660f, 840f);
		return new Vector2(width, height);
	}

	private static float GetRuneGridMinWidth(bool compactLayout)
	{
		float rowSeparation = compactLayout ? 6f : 8f;
		float cardMargins = (compactLayout ? 14f : 20f) * 2f;
		return RuneConfigColumns * RuneConfigCellWidth
			+ (RuneConfigColumns - 1) * rowSeparation
			+ cardMargins;
	}

	private static bool IsCompactConfigLayout()
	{
		Vector2I windowSize = DisplayServer.WindowGetSize();
		float windowHeight = windowSize.Y > 0 ? windowSize.Y : 720f;
		return windowHeight < CompactConfigHeightThreshold;
	}

	private static Vector2 GetTabButtonSize(bool compactLayout)
	{
		return compactLayout ? new Vector2(108f, 36f) : new Vector2(154f, 42f);
	}

	private static Color GetRarityAccentColor(HextechRarityTier rarity)
	{
		return rarity switch
		{
			HextechRarityTier.Silver => new Color(0.56f, 0.85f, 0.92f),
			HextechRarityTier.Prismatic => new Color(0.94f, 0.43f, 1f),
			_ => new Color(0.94f, 0.76f, 0.35f)
		};
	}

	private static Color GetRarityAccentColorByOrder(int rarityOrder)
	{
		return rarityOrder switch
		{
			0 => GetRarityAccentColor(HextechRarityTier.Silver),
			2 => GetRarityAccentColor(HextechRarityTier.Prismatic),
			_ => GetRarityAccentColor(HextechRarityTier.Gold)
		};
	}

	/// <summary>动作按钮与步进按钮共用的四态底板。</summary>
	private static void ApplyButtonStyles(Button button, bool includeFocus)
	{
		button.AddThemeStyleboxOverride("normal", CreateButtonStyle(HextechUiTheme.ButtonBackground, HextechUiTheme.ButtonBorder));
		button.AddThemeStyleboxOverride("hover", CreateButtonStyle(HextechUiTheme.ButtonBackgroundHover, HextechUiTheme.ButtonBorderHighlight));
		button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(HextechUiTheme.ButtonBackgroundPressed, HextechUiTheme.ButtonBorderPressed));
		if (includeFocus)
		{
			button.AddThemeStyleboxOverride("focus", CreateButtonStyle(HextechUiTheme.ButtonBackgroundHover, HextechUiTheme.ButtonBorderHighlight));
		}
	}

	private static StyleBoxFlat CreateButtonStyle(Color background, Color border)
	{
		StyleBoxFlat style = new()
		{
			BgColor = background,
			BorderColor = border,
			ShadowColor = new Color(0f, 0f, 0f, 0.24f),
			ShadowSize = 8,
			ShadowOffset = new Vector2(0f, 4f)
		};
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(8);
		style.ContentMarginLeft = 12;
		style.ContentMarginRight = 12;
		style.ContentMarginTop = 6;
		style.ContentMarginBottom = 6;
		return style;
	}

	private static StyleBoxFlat CreatePanelStyle()
	{
		StyleBoxFlat style = new()
		{
			BgColor = new Color(0.055f, 0.07f, 0.1f, 0.96f),
			BorderColor = new Color(0.86f, 0.74f, 0.42f, 0.72f),
			ShadowColor = new Color(0f, 0f, 0f, 0.42f),
			ShadowSize = 28,
			ShadowOffset = new Vector2(0f, 12f)
		};
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(18);
		return style;
	}

	private static StyleBoxFlat CreateCardStyle(Color? accent = null)
	{
		Color border = accent ?? new Color(0.48f, 0.55f, 0.66f, 0.34f);
		StyleBoxFlat style = new()
		{
			BgColor = new Color(0.09f, 0.11f, 0.16f, 0.55f),
			BorderColor = border,
			ShadowColor = new Color(0f, 0f, 0f, 0.22f),
			ShadowSize = 10,
			ShadowOffset = new Vector2(0f, 5f)
		};
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(16);
		return style;
	}

	private static StyleBoxFlat CreateTabShellStyle()
	{
		StyleBoxFlat style = new()
		{
			BgColor = new Color(0.07f, 0.085f, 0.12f, 0.92f),
			BorderColor = new Color(0.46f, 0.55f, 0.68f, 0.34f)
		};
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(12);
		style.ContentMarginLeft = 4;
		style.ContentMarginRight = 4;
		style.ContentMarginTop = 4;
		style.ContentMarginBottom = 4;
		return style;
	}

	private static StyleBoxFlat CreateTabSegmentStyle(bool active, bool hovered)
	{
		Color background = active
			? new Color(0.17f, 0.2f, 0.27f, 0.98f)
			: hovered ? new Color(0.13f, 0.16f, 0.22f, 0.82f) : HextechUiTheme.TransparentBlack;
		StyleBoxFlat style = new()
		{
			BgColor = background
		};
		style.SetCornerRadiusAll(9);
		// 激活页签的下划线由滑动指示条绘制,不画在单个按钮上。
		style.ContentMarginLeft = 10;
		style.ContentMarginRight = 10;
		style.ContentMarginTop = 5;
		style.ContentMarginBottom = 5;
		return style;
	}

	private static void StylePillTrack(Button toggle, float trackHeight)
	{
		int radius = (int)(trackHeight / 2f);
		// 轨道四态:关(深钢灰)/关悬停(略亮)/开(深金)/开悬停(更亮的金);旋钮颜色另由 panel stylebox 决定。
		toggle.AddThemeStyleboxOverride("normal", CreatePillTrackStyle(new Color(0.2f, 0.24f, 0.32f, 0.95f), HextechUiTheme.SoftSteelBorder, radius));
		toggle.AddThemeStyleboxOverride("hover", CreatePillTrackStyle(new Color(0.26f, 0.31f, 0.4f, 0.97f), new Color(0.62f, 0.7f, 0.82f, 0.66f), radius));
		toggle.AddThemeStyleboxOverride("pressed", CreatePillTrackStyle(new Color(0.86f, 0.66f, 0.28f, 0.98f), new Color(0.97f, 0.82f, 0.5f, 1f), radius));
		toggle.AddThemeStyleboxOverride("hover_pressed", CreatePillTrackStyle(new Color(0.94f, 0.74f, 0.34f, 1f), new Color(1f, 0.9f, 0.6f, 1f), radius));
		toggle.AddThemeStyleboxOverride("disabled", CreatePillTrackStyle(new Color(0.16f, 0.18f, 0.24f, 0.6f), new Color(0.34f, 0.38f, 0.46f, 0.4f), radius));
		toggle.AddThemeStyleboxOverride("focus", CreatePillFocusStyle(radius));
	}

	private static StyleBoxFlat CreatePillTrackStyle(Color background, Color border, int radius)
	{
		StyleBoxFlat style = new()
		{
			BgColor = background,
			BorderColor = border
		};
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(radius);
		return style;
	}

	private static StyleBoxFlat CreatePillFocusStyle(int radius)
	{
		StyleBoxFlat style = new()
		{
			BgColor = HextechUiTheme.TransparentBlack,
			BorderColor = new Color(0.96f, 0.82f, 0.5f, 0.95f)
		};
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(radius + 1);
		return style;
	}

	private static StyleBoxFlat CreatePillKnobStyle(float diameter)
	{
		StyleBoxFlat style = new()
		{
			BgColor = new Color(0.97f, 0.95f, 0.88f, 1f),
			ShadowColor = new Color(0f, 0f, 0f, 0.35f),
			ShadowSize = 3,
			ShadowOffset = new Vector2(0f, 1f)
		};
		style.SetCornerRadiusAll((int)(diameter / 2f));
		return style;
	}
}
