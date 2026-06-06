using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.addons.mega_text;

namespace HextechRunes;

internal sealed partial class HextechRuneSelectionScreen
{
	private TextureRect CreateRelicTexture(RelicModel relic, float sideLength)
	{
		TextureRect textureRect = new()
		{
			MouseFilter = MouseFilterEnum.Ignore,
			Texture = GetDisplayTexture(relic),
			CustomMinimumSize = new Vector2(sideLength, sideLength),
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize
		};
		return textureRect;
	}

	private MegaRichTextLabel CreateDescriptionLabel()
	{
		MegaRichTextLabel body = new()
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MaxFontSize = 20,
			MinFontSize = 15,
			BbcodeEnabled = true,
			MouseFilter = MouseFilterEnum.Ignore
		};
		ApplyDefaultMegaRichTextTheme(body);
		body.AddThemeColorOverride("default_color", new Color(0.9f, 0.93f, 0.97f, 0.92f));
		return body;
	}

	private static void SetFixedDescriptionText(MegaRichTextLabel label, string text, int fontSize)
	{
		label.MinFontSize = fontSize;
		label.MaxFontSize = fontSize;
		label.AddThemeFontSizeOverride("normal_font_size", fontSize);
		label.AddThemeFontSizeOverride("bold_font_size", fontSize);
		label.AddThemeFontSizeOverride("italics_font_size", fontSize);
		label.AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
		label.AddThemeFontSizeOverride("mono_font_size", fontSize);
		label.Text = text;
	}
}
