using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.addons.mega_text;

namespace HextechRunes;

internal sealed partial class HextechRuneSelectionScreen
{
	private Control CreateEnemyPreview()
	{
		PanelContainer panel = new()
		{
			Name = "EnemyPreviewPanel",
			CustomMinimumSize = new Vector2(1040f, 148f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore
		};
		panel.AddThemeStyleboxOverride("panel", CreatePreviewStyle());

		MarginContainer margin = new()
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		margin.AddThemeConstantOverride("margin_left", 18);
		margin.AddThemeConstantOverride("margin_right", 18);
		margin.AddThemeConstantOverride("margin_top", 16);
		margin.AddThemeConstantOverride("margin_bottom", 16);
		panel.AddChild(margin);

		HBoxContainer row = new()
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		row.AddThemeConstantOverride("separation", 18);
		margin.AddChild(row);

		CenterContainer iconBox = new()
		{
			CustomMinimumSize = new Vector2(96f, 96f),
			MouseFilter = MouseFilterEnum.Ignore
		};
		row.AddChild(iconBox);
		if (_monsterHexRelic != null && !_enemyHexRemoved)
		{
			TextureRect enemyTexture = CreateRelicTexture(_monsterHexRelic, 84f);
			iconBox.AddChild(enemyTexture);
			AttachRelicHoverTips(enemyTexture, _monsterHexRelic);
		}
		else
		{
			MegaLabel removedIcon = new()
			{
				Text = "-",
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MaxFontSize = 52,
				MinFontSize = 42
			};
			ApplyDefaultMegaLabelTheme(removedIcon);
			removedIcon.Modulate = new Color(0.86f, 0.88f, 0.92f, 0.68f);
			iconBox.AddChild(removedIcon);
		}

		VBoxContainer textColumn = new()
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		textColumn.AddThemeConstantOverride("separation", 5);
		row.AddChild(textColumn);

		MegaLabel eyebrow = new()
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			MaxFontSize = 15,
			MinFontSize = 12
		};
		ApplyDefaultMegaLabelTheme(eyebrow);
		eyebrow.Modulate = new Color(0.81f, 0.86f, 0.91f, 0.72f);
		eyebrow.SetTextAutoSize(new LocString(LocTable, "HEXTECH_ENEMY_PREVIEW_LABEL").GetRawText());
		textColumn.AddChild(eyebrow);

		HBoxContainer titleRow = new()
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		titleRow.AddThemeConstantOverride("separation", 10);
		textColumn.AddChild(titleRow);

		MegaLabel title = new()
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			MaxFontSize = 32,
			MinFontSize = 24
		};
		ApplyDefaultMegaLabelTheme(title);
		title.Modulate = new Color(0.97f, 0.96f, 0.9f, 0.96f);
		title.SetTextAutoSize(_monsterHexRelic != null && !_enemyHexRemoved
			? _monsterHexRelic.Title.GetFormattedText()
			: new LocString(LocTable, "HEXTECH_ENEMY_REMOVED_TITLE").GetRawText());
		titleRow.AddChild(title);

		if (_monsterHexRelic != null && !_enemyHexRemoved)
		{
			titleRow.AddChild(CreateRarityPill());
		}

		MegaRichTextLabel body = CreateDescriptionLabel();
		body.CustomMinimumSize = new Vector2(0f, 48f);
		if (_monsterHexKind.HasValue && !_enemyHexRemoved)
		{
			SetFixedDescriptionText(body, MonsterHexCatalog.GetEnemyHexDescriptionFormatted(_monsterHexKind.Value), 16);
		}
		else
		{
			SetFixedDescriptionText(body, new LocString(LocTable, "HEXTECH_ENEMY_REMOVED_DESCRIPTION").GetRawText(), 16);
		}
		textColumn.AddChild(body);

		if (_enemyHexControlsEnabled)
		{
			VBoxContainer actionColumn = new()
			{
				Name = "EnemyHexActionColumn",
				MouseFilter = MouseFilterEnum.Pass,
				CustomMinimumSize = new Vector2(148f, 0f),
				SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
				SizeFlagsVertical = SizeFlags.ExpandFill,
				Alignment = BoxContainer.AlignmentMode.Center
			};
			actionColumn.AddThemeConstantOverride("separation", 12);
			row.AddChild(actionColumn);

			Button rerollButton = CreateEnemyHexActionButton(new LocString(LocTable, "HEXTECH_REROLL").GetRawText());
			rerollButton.Disabled = _enemyHexRemoved || _enemyHexRerollFunc == null;
			rerollButton.Pressed += OnEnemyHexRerollPressed;
			actionColumn.AddChild(rerollButton);

			Button removeButton = CreateEnemyHexActionButton(new LocString(LocTable, _enemyHexRemoved ? "HEXTECH_ENEMY_UNDO_REMOVE" : "HEXTECH_ENEMY_REMOVE").GetRawText());
			removeButton.Disabled = _monsterHexKind == null && !_enemyHexRemoved;
			removeButton.Pressed += OnEnemyHexRemovePressed;
			actionColumn.AddChild(removeButton);
		}

		return panel;
	}

	private Button CreateEnemyHexActionButton(string text)
	{
		Color accent = GetAccentColor();
		Button button = new()
		{
			Text = text,
			FocusMode = FocusModeEnum.All,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			CustomMinimumSize = new Vector2(136f, 42f)
		};
		button.AddThemeStyleboxOverride("normal", CreateRerollStyle(new Color(0.08f, 0.1f, 0.15f, 0.72f), accent.Lightened(0.05f)));
		button.AddThemeStyleboxOverride("hover", CreateRerollStyle(new Color(0.1f, 0.13f, 0.18f, 0.82f), accent));
		button.AddThemeStyleboxOverride("pressed", CreateRerollStyle(new Color(0.07f, 0.09f, 0.13f, 0.86f), accent.Lightened(0.12f)));
		button.AddThemeStyleboxOverride("focus", CreateRerollStyle(new Color(0.1f, 0.13f, 0.18f, 0.82f), accent));
		button.AddThemeStyleboxOverride("disabled", CreateRerollStyle(new Color(0.08f, 0.09f, 0.12f, 0.56f), accent.Darkened(0.35f)));
		return button;
	}
}
