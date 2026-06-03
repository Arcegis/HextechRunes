using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

internal sealed partial class HextechRuneSelectionScreen
{
	private Control CreateRarityPill()
	{
		return CreateTextPill(new LocString(LocTable, "HEXTECH_SERIES." + _rarityKey).GetRawText());
	}

	private Control CreatePlayerPoolPill(RelicModel relic)
	{
		string poolKey = HextechCatalog.GetPlayerRunePoolKey(relic);
		return CreateTextPill(new LocString(LocTable, "HEXTECH_POOL." + poolKey).GetRawText());
	}

	private Control CreatePlayerTagPill(RelicModel relic)
	{
		string tagKey = HextechCatalog.GetPlayerRuneTagKey(relic);
		return CreateTextPill(new LocString(LocTable, "HEXTECH_TAG." + tagKey).GetRawText());
	}

	private Control CreatePlayerMetadataPills(RelicModel relic)
	{
		Control wrapper = new()
		{
			MouseFilter = MouseFilterEnum.Ignore,
			CustomMinimumSize = new Vector2(0f, 24f)
		};

		CenterContainer pillCenter = new()
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		pillCenter.AnchorLeft = 0f;
		pillCenter.AnchorRight = 1f;
		pillCenter.AnchorTop = 0f;
		pillCenter.AnchorBottom = 1f;
		pillCenter.OffsetTop = -4f;
		pillCenter.OffsetBottom = -4f;

		HBoxContainer row = new()
		{
			MouseFilter = MouseFilterEnum.Ignore,
			Alignment = BoxContainer.AlignmentMode.Center
		};
		row.AddThemeConstantOverride("separation", 6);
		row.AddChild(CreatePlayerPoolPill(relic));
		row.AddChild(CreatePlayerTagPill(relic));
		pillCenter.AddChild(row);
		wrapper.AddChild(pillCenter);
		return wrapper;
	}

	private Control CreateTextPill(string text)
	{
		PanelContainer pill = new()
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		pill.AddThemeStyleboxOverride("panel", CreatePillStyle(GetAccentColor()));

		Label label = new()
		{
			MouseFilter = MouseFilterEnum.Ignore,
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		label.AddThemeFontSizeOverride("font_size", 14);
		label.AddThemeColorOverride("font_color", new Color(0.08f, 0.09f, 0.11f, 0.92f));
		pill.AddChild(label);
		return pill;
	}
}
