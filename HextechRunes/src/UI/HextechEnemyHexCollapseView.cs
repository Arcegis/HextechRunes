using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;

namespace HextechRunes;

// 折叠模式的敌方海克斯 UI(配置「折叠敌方海克斯」开启时用):顶栏「地图按钮」左侧一个与右侧图标同尺寸的按钮,
// 外观 = 深色底 + 「最新获得的那个敌方海克斯」图标 + 右下角数字角标(当前敌方海克斯总数,像牌组「10」)。
// 点击在按钮下方展开/收起一个窗口:**每幕独占一行**(第 N 行 = 第 N 幕的海克斯),深色底宽度 = 各幕海克斯数量最大值
// (用补空占位把每行凑满「保留列数」实现;例如每幕 1/2/3 则留 3 列宽)。
//
// 面板挂进 NGame.HoverTipsContainer 并置为第 0 个子节点(不用 TopLevel,否则会画在提示之上)——动态 append 的 hover
// 提示排在面板之后、画在其上,面板深色底不挡描述。与旧版顶栏平铺 strip 互斥。纯表现层,异常由 Refresh 的 try/catch 兜住。
internal static class HextechEnemyHexCollapseView
{
	private const string ButtonName = "HextechEnemyHexCollapseButton";
	private const string PanelName = "HextechEnemyHexCollapsePanel";
	private const string GridName = "HextechEnemyHexCollapseGrid";
	private const string MapButtonTypeName = "NTopBarMapButton";
	private const string DeckButtonTypeName = "NTopBarDeckButton";
	private static readonly Vector2 FallbackButtonSize = new(52f, 52f);
	private static readonly Vector2 FallbackCellSize = new(68f, 68f);

	private static Button? _button;
	private static TextureRect? _iconRect;
	private static Label? _countBadge;
	private static PanelContainer? _panel;
	private static GridContainer? _grid;
	private static bool _open;
	private static Vector2 _cellSize = Vector2.Zero;

	// 展示折叠视图。hexRows 为每幕一行;全空则隐藏。
	internal static void Show(IReadOnlyList<IReadOnlyList<MonsterHexKind>> hexRows, int reservedColumns)
	{
		int total = 0;
		foreach (IReadOnlyList<MonsterHexKind> row in hexRows)
		{
			total += row.Count;
		}

		if (total == 0)
		{
			Remove();
			return;
		}

		EnsureButton();
		if (_button == null || !GodotObject.IsInstanceValid(_button))
		{
			return;
		}

		EnsurePanel();
		RebuildRows(hexRows, reservedColumns);
		UpdateButtonIcon(LatestHex(hexRows));

		if (_countBadge != null && GodotObject.IsInstanceValid(_countBadge))
		{
			_countBadge.Text = total.ToString();
		}

		_button.Visible = true;
		UpdatePanel();
	}

	// 切回旧版或清理:移除按钮与面板。
	internal static void Remove()
	{
		QueueFreeIfValid(_button);
		QueueFreeIfValid(_panel);
		_button = null;
		_iconRect = null;
		_countBadge = null;
		_panel = null;
		_grid = null;
		_open = false;
	}

	private static MonsterHexKind LatestHex(IReadOnlyList<IReadOnlyList<MonsterHexKind>> hexRows)
	{
		IReadOnlyList<MonsterHexKind> lastRow = hexRows[hexRows.Count - 1];
		return lastRow[lastRow.Count - 1];
	}

	private static void QueueFreeIfValid(Node? node)
	{
		if (node != null && GodotObject.IsInstanceValid(node))
		{
			node.QueueFree();
		}
	}

	private static void EnsureButton()
	{
		if (_button != null && GodotObject.IsInstanceValid(_button))
		{
			return;
		}

		Node? mapButton = FindMapButton();
		Node? parent = mapButton?.GetParent();
		if (mapButton == null || parent == null)
		{
			return;
		}

		Vector2 iconSize = mapButton is Control mapControl ? mapControl.Size : Vector2.Zero;
		if (iconSize.X < 8f || iconSize.Y < 8f)
		{
			iconSize = FallbackButtonSize;
		}

		Button button = new()
		{
			Name = ButtonName,
			FocusMode = Control.FocusModeEnum.None,
			MouseFilter = Control.MouseFilterEnum.Stop,
			CustomMinimumSize = iconSize,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};
		button.AddThemeStyleboxOverride("normal", CreateButtonStyle(0.62f));
		button.AddThemeStyleboxOverride("hover", CreateButtonStyle(0.82f));
		button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(0.9f));
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		button.Pressed += OnButtonPressed;

		TextureRect icon = new()
		{
			Name = "HexIcon",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize
		};
		icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		icon.OffsetLeft = 5f;
		icon.OffsetTop = 4f;
		icon.OffsetRight = -5f;
		icon.OffsetBottom = -4f;
		button.AddChild(icon);

		Label badge = new()
		{
			Name = "CountBadge",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom
		};
		badge.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		badge.OffsetRight = -3f;
		badge.OffsetBottom = -1f;
		badge.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
		badge.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.95f));
		badge.AddThemeConstantOverride("outline_size", 5);
		badge.AddThemeFontSizeOverride("font_size", Mathf.Max(14, (int)(iconSize.Y * 0.4f)));
		button.AddChild(badge);

		parent.AddChild(button);
		parent.MoveChild(button, mapButton.GetIndex());

		// 角标数字字体照抄原版牌组计数,渲染完全一致(需先入树,主题才可解析)。
		ApplyDeckCountFont(badge);

		_button = button;
		_iconRect = icon;
		_countBadge = badge;
	}

	// 把原版牌组按钮计数标签的字体/字号/颜色/描边照抄到角标,使数字与牌组「10」渲染一致。取不到就保留角标自带兜底样式。
	private static void ApplyDeckCountFont(Label badge)
	{
		try
		{
			Node? deckButton = FindDeckButton();
			Label? deckLabel = deckButton == null ? null : FindFirstLabel(deckButton);
			if (deckLabel == null || !GodotObject.IsInstanceValid(deckLabel))
			{
				return;
			}

			Font? font = deckLabel.GetThemeFont("font");
			if (font != null)
			{
				badge.AddThemeFontOverride("font", font);
			}

			int fontSize = deckLabel.GetThemeFontSize("font_size");
			if (fontSize > 0)
			{
				badge.AddThemeFontSizeOverride("font_size", fontSize);
			}

			badge.AddThemeColorOverride("font_color", deckLabel.GetThemeColor("font_color"));
			badge.AddThemeColorOverride("font_outline_color", deckLabel.GetThemeColor("font_outline_color"));
			badge.AddThemeConstantOverride("outline_size", deckLabel.GetThemeConstant("outline_size"));
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] CollapseView: copy deck count font failed: {ex.Message}");
		}
	}

	private static Node? FindDeckButton()
	{
		Node? topBar = NRun.Instance?.GlobalUi?.TopBar;
		return topBar == null ? null : FindDescendantByTypeName(topBar, DeckButtonTypeName);
	}

	private static Label? FindFirstLabel(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is Label label)
			{
				return label;
			}

			Label? found = FindFirstLabel(child);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	private static void UpdateButtonIcon(MonsterHexKind latestHex)
	{
		if (_iconRect == null || !GodotObject.IsInstanceValid(_iconRect))
		{
			return;
		}

		Texture2D? texture = TryLoadHexIcon(latestHex);
		if (texture != null)
		{
			_iconRect.Texture = texture;
		}
	}

	private static Texture2D? TryLoadHexIcon(MonsterHexKind hex)
	{
		try
		{
			RelicModel relic = MonsterHexCatalog.GetIconRelicForMonsterHex(hex);
			string path = relic.PackedIconPath;
			if (!string.IsNullOrEmpty(path) && ResourceLoader.Exists(path))
			{
				return ResourceLoader.Load<Texture2D>(path);
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] CollapseView: failed to load latest hex icon {hex}: {ex.Message}");
		}

		return null;
	}

	private static void EnsurePanel()
	{
		if (_panel != null && GodotObject.IsInstanceValid(_panel) && _grid != null && GodotObject.IsInstanceValid(_grid))
		{
			return;
		}

		QueueFreeIfValid(_panel);

		PanelContainer panel = new()
		{
			Name = PanelName,
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());

		MarginContainer margin = new()
		{
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		margin.AddThemeConstantOverride("margin_left", 10);
		margin.AddThemeConstantOverride("margin_right", 10);
		margin.AddThemeConstantOverride("margin_top", 8);
		margin.AddThemeConstantOverride("margin_bottom", 8);

		GridContainer grid = new()
		{
			Name = GridName,
			Columns = 1,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		grid.AddThemeConstantOverride("h_separation", 4);
		grid.AddThemeConstantOverride("v_separation", 4);

		margin.AddChild(grid);
		panel.AddChild(margin);

		Node? host = NGame.Instance?.HoverTipsContainer ?? FindMapButton()?.GetParent();
		if (host != null)
		{
			host.AddChild(panel);
			host.MoveChild(panel, 0);
		}

		_panel = panel;
		_grid = grid;
	}

	// 按幕铺格:每幕一行,不足「保留列数」的用空占位补满,使每幕独占一行、深色底宽度稳定为保留列数。
	private static void RebuildRows(IReadOnlyList<IReadOnlyList<MonsterHexKind>> hexRows, int reservedColumns)
	{
		if (_grid == null || !GodotObject.IsInstanceValid(_grid))
		{
			return;
		}

		int columns = Math.Max(1, reservedColumns);
		_grid.Columns = columns;

		foreach (Node child in _grid.GetChildren())
		{
			// 包装格里的 holder 在被 free 时会经 TreeExiting 自行 NHoverTipSet.Remove,这里直接释放整格。
			_grid.RemoveChild(child);
			child.QueueFree();
		}

		Vector2 cell = ResolveCellSize(hexRows);

		foreach (IReadOnlyList<MonsterHexKind> row in hexRows)
		{
			for (int i = 0; i < columns; i++)
			{
				if (i < row.Count)
				{
					try
					{
						Control holder = HextechEnemyUi.CreateEnemyHexHolder(row[i]);
						holder.Scale = Vector2.One; // 折叠面板里保持原始大小,不缩小图标(需要更多空间时靠背景变大)
						_grid.AddChild(holder);
						continue;
					}
					catch (Exception ex)
					{
						Log.Warn($"[{ModInfo.Id}][Mayhem] CollapseView: skipped enemy hex icon {row[i]}: {ex.Message}");
					}
				}

				_grid.AddChild(EmptyCell(cell));
			}
		}
	}

	private static Control EmptyCell(Vector2 cellSize)
	{
		return new Control
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			CustomMinimumSize = cellSize
		};
	}

	// 量一个真实 holder 的自然尺寸 × 缩放得到格子尺寸;量不到就用兜底值。量到一次后缓存。
	private static Vector2 ResolveCellSize(IReadOnlyList<IReadOnlyList<MonsterHexKind>> hexRows)
	{
		if (_cellSize.X > 4f && _cellSize.Y > 4f)
		{
			return _cellSize;
		}

		try
		{
			MonsterHexKind sample = hexRows[0][0];
			Control holder = HextechEnemyUi.CreateEnemyHexHolder(sample);
			Vector2 natural = holder.GetCombinedMinimumSize();
			holder.QueueFree();
			if (natural.X > 4f && natural.Y > 4f)
			{
				_cellSize = natural;
				return _cellSize;
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] CollapseView: cell size probe failed: {ex.Message}");
		}

		return FallbackCellSize;
	}

	private static void OnButtonPressed()
	{
		_open = !_open;
		UpdatePanel();
	}

	private static void UpdatePanel()
	{
		if (_panel == null || !GodotObject.IsInstanceValid(_panel))
		{
			return;
		}

		_panel.Visible = _open;
		if (_open)
		{
			PositionPanel();
		}
	}

	private static void PositionPanel()
	{
		if (_panel == null || _button == null
			|| !GodotObject.IsInstanceValid(_panel) || !GodotObject.IsInstanceValid(_button))
		{
			return;
		}

		Rect2 buttonRect = _button.GetGlobalRect();
		float panelWidth = _panel.Size.X > 8f ? _panel.Size.X : 200f;
		float screenWidth = _panel.GetViewportRect().Size.X;

		float x = buttonRect.Position.X;
		if (x + panelWidth > screenWidth - 8f)
		{
			x = screenWidth - 8f - panelWidth;
		}

		if (x < 8f)
		{
			x = 8f;
		}

		_panel.GlobalPosition = new Vector2(x, buttonRect.End.Y + 6f);
	}

	private static Node? FindMapButton()
	{
		Node? topBar = NRun.Instance?.GlobalUi?.TopBar;
		return topBar == null ? null : FindDescendantByTypeName(topBar, MapButtonTypeName);
	}

	private static Node? FindDescendantByTypeName(Node node, string typeName)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child.GetType().Name == typeName)
			{
				return child;
			}

			Node? found = FindDescendantByTypeName(child, typeName);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	private static StyleBoxFlat CreateButtonStyle(float bgAlpha)
	{
		StyleBoxFlat style = new()
		{
			BgColor = new Color(0.05f, 0.06f, 0.09f, bgAlpha),
			BorderColor = new Color(0.42f, 0.48f, 0.6f, 0.42f)
		};
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(8);
		return style;
	}

	private static StyleBoxFlat CreatePanelStyle()
	{
		StyleBoxFlat style = new()
		{
			BgColor = new Color(0.035f, 0.045f, 0.07f, 0.94f),
			BorderColor = new Color(0.4f, 0.46f, 0.56f, 0.5f)
		};
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(10);
		return style;
	}
}
