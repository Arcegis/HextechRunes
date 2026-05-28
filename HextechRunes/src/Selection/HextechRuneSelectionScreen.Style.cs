using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.addons.mega_text;

namespace HextechRunes;

internal sealed partial class HextechRuneSelectionScreen : Control, IOverlayScreen, IScreenContext
{
	private static string DetermineRarityKey(IReadOnlyList<RelicModel> relics)
	{
		if (relics.Count == 0)
		{
			return "GOLD";
		}

		Type? relicType = HextechCatalog.GetPlayerRuneTypesForRarity(HextechRarityTier.Silver).FirstOrDefault(type => ModelDb.GetId(type) == (relics[0].CanonicalInstance?.Id ?? relics[0].Id));
		if (relicType != null)
		{
			return "SILVER";
		}

		relicType = HextechCatalog.GetPlayerRuneTypesForRarity(HextechRarityTier.Prismatic).FirstOrDefault(type => ModelDb.GetId(type) == (relics[0].CanonicalInstance?.Id ?? relics[0].Id));
		return relicType != null ? "PRISMATIC" : "GOLD";
	}

	private Color GetAccentColor()
	{
		return _rarityKey switch
		{
			"SILVER" => new Color(0.56f, 0.85f, 0.92f),
			"PRISMATIC" => new Color(0.94f, 0.43f, 1f),
			_ => new Color(0.94f, 0.76f, 0.35f)
		};
	}

	private static Texture2D? GetDisplayTexture(RelicModel relic)
	{
		return relic.BigIcon ?? relic.Icon;
	}

	private static StyleBoxFlat CreateContentPanelStyle()
	{
		StyleBoxFlat style = new();
		style.BgColor = new Color(0.04f, 0.05f, 0.08f, 0.4f);
		style.BorderColor = new Color(0.48f, 0.55f, 0.66f, 0.35f);
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(28);
		style.ContentMarginLeft = 8;
		style.ContentMarginRight = 8;
		style.ContentMarginTop = 8;
		style.ContentMarginBottom = 8;
		style.ShadowColor = new Color(0f, 0f, 0f, 0.26f);
		style.ShadowSize = 18;
		style.ShadowOffset = new Vector2(0f, 10f);
		return style;
	}

	private static StyleBoxFlat CreatePreviewStyle()
	{
		StyleBoxFlat style = new();
		style.BgColor = new Color(0.07f, 0.09f, 0.13f, 0.48f);
		style.BorderColor = new Color(0.72f, 0.42f, 0.42f, 0.55f);
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(20);
		style.ContentMarginLeft = 6;
		style.ContentMarginRight = 6;
		style.ContentMarginTop = 6;
		style.ContentMarginBottom = 6;
		style.ShadowColor = new Color(0f, 0f, 0f, 0.16f);
		style.ShadowSize = 10;
		style.ShadowOffset = new Vector2(0f, 6f);
		return style;
	}

	private static StyleBoxFlat CreateCardStyle(Color background, Color border, int borderWidth, float shadowAlpha)
	{
		StyleBoxFlat style = new();
		style.BgColor = background;
		style.BorderColor = border;
		style.SetBorderWidthAll(borderWidth);
		style.SetCornerRadiusAll(26);
		style.ContentMarginLeft = 6;
		style.ContentMarginRight = 6;
		style.ContentMarginTop = 6;
		style.ContentMarginBottom = 6;
		style.ShadowColor = new Color(0f, 0f, 0f, shadowAlpha);
		style.ShadowSize = 16;
		style.ShadowOffset = new Vector2(0f, 10f);
		return style;
	}

	private static StyleBoxFlat CreateRerollStyle(Color background, Color border)
	{
		StyleBoxFlat style = new();
		style.BgColor = background;
		style.BorderColor = border;
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(18);
		style.ContentMarginLeft = 6;
		style.ContentMarginRight = 6;
		style.ContentMarginTop = 4;
		style.ContentMarginBottom = 4;
		style.ShadowColor = new Color(0f, 0f, 0f, 0.16f);
		style.ShadowSize = 8;
		style.ShadowOffset = new Vector2(0f, 4f);
		return style;
	}

	private static void ApplyRerollButtonVisualState(Button button, TextureRect icon, bool alreadyRerolled)
	{
		if (!alreadyRerolled)
		{
			button.Modulate = Colors.White;
			icon.SelfModulate = Colors.White;
			return;
		}

		button.Modulate = new Color(0.62f, 0.64f, 0.68f, 0.82f);
		icon.SelfModulate = new Color(0.46f, 0.49f, 0.54f, 0.95f);
	}

	private static StyleBoxFlat CreatePillStyle(Color accent)
	{
		Color background = accent.Lightened(0.24f);
		background.A = 0.78f;
		Color border = accent.Lightened(0.34f);
		border.A = 0.72f;

		StyleBoxFlat style = new();
		style.BgColor = background;
		style.BorderColor = border;
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(7);
		style.ContentMarginLeft = 8;
		style.ContentMarginRight = 8;
		style.ContentMarginTop = 3;
		style.ContentMarginBottom = 3;
		return style;
	}

	private static void ApplyDefaultMegaLabelTheme(MegaLabel label)
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

	private static void ApplyDefaultMegaRichTextTheme(MegaRichTextLabel label)
	{
		Font font = label.GetThemeDefaultFont();
		if (font != null)
		{
			label.AddThemeFontOverride("normal_font", font);
			label.AddThemeFontOverride("bold_font", font);
			label.AddThemeFontOverride("italics_font", font);
			label.AddThemeFontOverride("bold_italics_font", font);
			label.AddThemeFontOverride("mono_font", font);
		}

		int fontSize = label.GetThemeDefaultFontSize();
		if (fontSize > 0)
		{
			label.AddThemeFontSizeOverride("normal_font_size", fontSize);
			label.AddThemeFontSizeOverride("bold_font_size", fontSize);
			label.AddThemeFontSizeOverride("italics_font_size", fontSize);
			label.AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
			label.AddThemeFontSizeOverride("mono_font_size", fontSize);
		}
	}

	private static void SetMouseFilterIgnoreRecursive(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is Control control)
			{
				control.MouseFilter = MouseFilterEnum.Ignore;
			}

			SetMouseFilterIgnoreRecursive(child);
		}
	}
}
