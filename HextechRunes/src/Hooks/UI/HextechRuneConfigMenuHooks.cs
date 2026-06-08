using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechRuneConfigMenuHooks
{
	private const string LocTable = "relic_collection";
	private const string ButtonName = "HextechRuneConfigButton";
	private const string OverlayName = "HextechRuneConfigOverlay";
	private const int MaxAttachAttempts = 30;
	private const int NativeDuplicateFlags = 14;
	private const int OverlayZIndex = 1000;
	private const int HoverTipZIndex = 2000;
	private const float ConfigRuneHolderScale = 0.84f;
	private static readonly FieldInfo? MainMenuButtonLocStringField = TryGetField(typeof(NMainMenuTextButton), "_locString");
	private static readonly FieldInfo? MainMenuLastHitButtonField = TryGetField(typeof(NMainMenu), "_lastHitButton");
	private static readonly MethodInfo? MainMenuButtonFocusedMethod = TryGetMethod(typeof(NMainMenu), "MainMenuButtonFocused", BindingFlags.Instance | BindingFlags.NonPublic, typeof(NMainMenuTextButton));
	private static readonly MethodInfo? MainMenuButtonUnfocusedMethod = TryGetMethod(typeof(NMainMenu), "MainMenuButtonUnfocused", BindingFlags.Instance | BindingFlags.NonPublic, typeof(NMainMenuTextButton));

	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(NMainMenu), nameof(NMainMenu._Ready), BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechRuneConfigMenuHooks), nameof(MainMenuReadyPostfix)));
	}

	private static void MainMenuReadyPostfix(NMainMenu __instance)
	{
		_ = AttachButtonWhenReadyAsync(__instance);
	}

	private static async Task AttachButtonWhenReadyAsync(NMainMenu mainMenu)
	{
		for (int attempt = 1; attempt <= MaxAttachAttempts; attempt++)
		{
			if (!GodotObject.IsInstanceValid(mainMenu))
			{
				return;
			}

			try
			{
				if (TryAttachButton(mainMenu))
				{
					return;
				}
			}
			catch (Exception ex)
			{
				Log.Warn($"[{ModInfo.Id}][RuneConfig] Main menu button install failed: {ex.Message}", 2);
				return;
			}

			if (!await AwaitProcessFrameAsync(mainMenu))
			{
				return;
			}
		}

		Log.Warn($"[{ModInfo.Id}][RuneConfig] Main menu button skipped: root was not ready.", 2);
	}

	private static bool TryAttachButton(NMainMenu host)
	{
		if (host.GetNodeOrNull<NMainMenuTextButton>(ButtonName) is { } existingNative
			&& GodotObject.IsInstanceValid(existingNative))
		{
			return true;
		}

		if (TryAttachNativeMenuButton(host))
		{
			Log.Info($"[{ModInfo.Id}][RuneConfig] Main menu config button attached.");
			return true;
		}

		Log.Warn($"[{ModInfo.Id}][RuneConfig] Main menu config button skipped: native menu buttons were not available.", 2);
		return false;
	}

	private static bool TryAttachNativeMenuButton(NMainMenu mainMenu)
	{
		if (MainMenuButtonLocStringField == null)
		{
			return false;
		}

		if (!HextechUpdateChecker.TryFindNoticeLayer(mainMenu, out _, out Label template, out Node noticeHost)
			|| mainMenu.GetNodeOrNull<NMainMenuTextButton>("MainMenuTextButtons/SettingsButton") is not { } settingsButton)
		{
			return false;
		}

		NMainMenuTextButton configButton = (NMainMenuTextButton)((Node)settingsButton).Duplicate(NativeDuplicateFlags);
		((Node)configButton).Name = ButtonName;
		((Node)configButton).UniqueNameInOwner = true;
		noticeHost.AddChild(configButton);
		noticeHost.MoveChild(configButton, Math.Min(template.GetIndex() + 2, noticeHost.GetChildCount() - 1));
		ConfigureNativeMenuLabel(configButton);
		ConfigureStatusLayerMenuButton(configButton, template);
		ConfigureNativeMenuFocus(mainMenu, configButton);
		ConnectNativeMenuButton(configButton);
		return true;
	}

	private static void ConfigureNativeMenuLabel(NMainMenuTextButton configButton)
	{
		MainMenuButtonLocStringField?.SetValue(configButton, null);
		if (((Node)configButton).GetChildCount() > 0 && ((Node)configButton).GetChild(0) is Label label)
		{
			label.Text = L("HEXTECH_CONFIG_BUTTON");
			label.HorizontalAlignment = HorizontalAlignment.Left;
			label.AnchorLeft = 0f;
			label.AnchorRight = 1f;
			label.OffsetLeft = 0f;
			label.OffsetRight = 0f;
			label.PivotOffset = label.Size * 0.5f;
		}

		((Control)configButton).TooltipText = L("HEXTECH_CONFIG_BUTTON_TOOLTIP");
	}

	private static void ConfigureStatusLayerMenuButton(NMainMenuTextButton configButton, Label template)
	{
		Control control = configButton;
		control.SetAnchorsPreset(Control.LayoutPreset.BottomLeft, false);
		control.OffsetLeft = 0f;
		control.OffsetRight = 420f;
		control.OffsetTop = -92f;
		control.OffsetBottom = -48f;
		control.MouseFilter = Control.MouseFilterEnum.Stop;
		control.FocusMode = Control.FocusModeEnum.All;
		control.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
		control.ZIndex = template.ZIndex;
		control.ZAsRelative = template.ZAsRelative;
	}

	private static void ConfigureNativeMenuFocus(NMainMenu mainMenu, NMainMenuTextButton configButton)
	{
		if (MainMenuButtonFocusedMethod != null)
		{
			((GodotObject)configButton).Connect(
				NClickableControl.SignalName.Focused,
				Callable.From<NMainMenuTextButton>(button =>
				{
					Callable.From(() => MainMenuButtonFocusedMethod.Invoke(mainMenu, [button])).CallDeferred();
				}));
		}

		if (MainMenuButtonUnfocusedMethod != null)
		{
			((GodotObject)configButton).Connect(
				NClickableControl.SignalName.Unfocused,
				Callable.From<NMainMenuTextButton>(button => MainMenuButtonUnfocusedMethod.Invoke(mainMenu, [button])));
		}
	}

	private static void ConnectNativeMenuButton(NMainMenuTextButton configButton)
	{
		((GodotObject)configButton).Connect(
			NClickableControl.SignalName.Released,
			Callable.From<NButton>(_ =>
			{
				if (FindAncestor<NMainMenu>(configButton) is { } mainMenu)
				{
					MainMenuLastHitButtonField?.SetValue(mainMenu, configButton);
				}

				OpenOverlay(configButton);
			}));
	}

	private static void OpenOverlay(Node source)
	{
		Node root = ResolveRoot(source);
		RemoveExistingOverlay(root);
		Control overlay = CreateOverlay();
		root.AddChild(overlay);
	}

	private static Control CreateOverlay()
	{
		Control overlay = new()
		{
			Name = OverlayName,
			MouseFilter = Control.MouseFilterEnum.Stop,
			ZIndex = OverlayZIndex
		};
		overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		ColorRect shade = new()
		{
			Color = new Color(0f, 0f, 0f, 0.72f),
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		overlay.AddChild(shade);

		CenterContainer center = new()
		{
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		overlay.AddChild(center);

			PanelContainer panel = new()
			{
				CustomMinimumSize = new Vector2(980f, 700f),
				MouseFilter = Control.MouseFilterEnum.Stop
			};
		panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());
		center.AddChild(panel);

		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 28);
		margin.AddThemeConstantOverride("margin_right", 28);
		margin.AddThemeConstantOverride("margin_top", 24);
		margin.AddThemeConstantOverride("margin_bottom", 24);
		panel.AddChild(margin);

		VBoxContainer content = new()
		{
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		content.AddThemeConstantOverride("separation", 14);
		margin.AddChild(content);

		Label title = CreateLabel(L("HEXTECH_CONFIG_TITLE"), 30, new Color(0.98f, 0.94f, 0.82f, 1f));
		title.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(title);

			Label description = CreateLabel(L("HEXTECH_CONFIG_DESCRIPTION"), 16, new Color(0.82f, 0.86f, 0.92f, 0.92f));
			description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			content.AddChild(description);

			HashSet<string> pendingDisabledIds = HextechRuneConfiguration.GetDisabledPlayerRuneIds().ToHashSet(StringComparer.Ordinal);
			List<RuneIconBinding> iconBindings = [];
			Label summary = CreateLabel(string.Empty, 16, new Color(0.92f, 0.88f, 0.7f, 0.95f));
			content.AddChild(CreateToolbar(overlay, pendingDisabledIds, iconBindings, summary));
			content.AddChild(summary);

		ScrollContainer scroll = new()
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Stop
		};
			VBoxContainer list = new()
			{
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			list.AddThemeConstantOverride("separation", 16);
			scroll.AddChild(list);
			content.AddChild(scroll);

			foreach (IGrouping<int, RuneConfigEntry> rarityGroup in BuildRuneEntries().GroupBy(static entry => entry.RarityOrder))
			{
				list.AddChild(CreateSectionHeader(rarityGroup.First().RarityText));
				GridContainer grid = CreateRuneGrid();
				list.AddChild(grid);
				foreach (RuneConfigEntry entry in rarityGroup)
				{
					RuneIconBinding binding = CreateRuneIcon(entry, pendingDisabledIds, summary);
					iconBindings.Add(binding);
					grid.AddChild(binding.Root);
				}
			}

		UpdateSummary(summary, pendingDisabledIds);
		return overlay;
	}

	private static Control CreateToolbar(
		Control overlay,
		HashSet<string> pendingDisabledIds,
		IReadOnlyList<RuneIconBinding> iconBindings,
		Label summary)
	{
		HBoxContainer toolbar = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		toolbar.AddThemeConstantOverride("separation", 12);

			toolbar.AddChild(CreateActionButton(L("HEXTECH_CONFIG_ENABLE_ALL"), () =>
			{
				pendingDisabledIds.Clear();
				UpdateAllRuneIcons(iconBindings, pendingDisabledIds);
				UpdateSummary(summary, pendingDisabledIds);
			}));
			toolbar.AddChild(CreateActionButton(L("HEXTECH_CONFIG_DISABLE_ALL"), () =>
			{
				foreach (RuneConfigEntry entry in BuildRuneEntries())
			{
					pendingDisabledIds.Add(entry.Id);
				}

				UpdateAllRuneIcons(iconBindings, pendingDisabledIds);
				UpdateSummary(summary, pendingDisabledIds);
			}));
			toolbar.AddChild(CreateActionButton(L("HEXTECH_CONFIG_RESET"), () =>
			{
				pendingDisabledIds.Clear();
				pendingDisabledIds.UnionWith(HextechRuneConfiguration.GetDefaultDisabledPlayerRuneIds());
				UpdateAllRuneIcons(iconBindings, pendingDisabledIds);
				UpdateSummary(summary, pendingDisabledIds);
			}));
		toolbar.AddChild(CreateActionButton(L("HEXTECH_CONFIG_SAVE_CLOSE"), () =>
		{
			HextechRuneConfiguration.SaveDisabledPlayerRuneIds(pendingDisabledIds);
			Log.Info($"[{ModInfo.Id}][RuneConfig] Saved player rune config: disabled={pendingDisabledIds.Count}");
			overlay.QueueFree();
		}));
		toolbar.AddChild(CreateActionButton(L("HEXTECH_CONFIG_CANCEL"), overlay.QueueFree));
		return toolbar;
	}

	private static Label CreateSectionHeader(string text)
	{
		Label label = CreateLabel(text, 20, new Color(0.96f, 0.84f, 0.48f, 0.98f));
		label.CustomMinimumSize = new Vector2(0f, 26f);
		return label;
	}

	private static GridContainer CreateRuneGrid()
	{
		GridContainer grid = new()
		{
			Columns = 9,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		grid.AddThemeConstantOverride("h_separation", 14);
		grid.AddThemeConstantOverride("v_separation", 14);
		return grid;
	}

	private static RuneIconBinding CreateRuneIcon(RuneConfigEntry entry, HashSet<string> pendingDisabledIds, Label summary)
	{
		VBoxContainer root = new()
		{
			Name = "RuneConfigIcon_" + entry.Id,
			CustomMinimumSize = new Vector2(96f, 126f),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			MouseFilter = Control.MouseFilterEnum.Stop,
			FocusMode = Control.FocusModeEnum.All,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			Alignment = BoxContainer.AlignmentMode.Center
		};
		root.AddThemeConstantOverride("separation", 5);

		CenterContainer iconCenter = new()
		{
			CustomMinimumSize = new Vector2(96f, 82f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		NRelicBasicHolder holder = NRelicBasicHolder.Create(entry.Relic)
			?? throw new InvalidOperationException($"Failed to create config relic holder for {entry.Id}.");
		holder.Scale = Vector2.One * ConfigRuneHolderScale;
		holder.MouseFilter = Control.MouseFilterEnum.Ignore;
		iconCenter.AddChild(holder);
		root.AddChild(iconCenter);

		Label title = CreateRuneNameLabel(entry.Title);
		root.AddChild(title);

		RuneIconBinding binding = new(entry.Id, root);
		ApplyRuneIconState(binding, !pendingDisabledIds.Contains(entry.Id));
		root.GuiInput += inputEvent =>
		{
			if (IsRuneToggleInput(inputEvent))
			{
				root.GetViewport().SetInputAsHandled();
				ToggleRune(entry.Id, binding, pendingDisabledIds, summary);
			}
		};
		AttachRelicHoverTips(root, entry.Relic);
		return binding;
	}

	private static Label CreateRuneNameLabel(string text)
	{
		Label label = CreateLabel(text, 12, new Color(0.9f, 0.92f, 0.96f, 0.95f));
		label.CustomMinimumSize = new Vector2(96f, 34f);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Top;
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.ClipText = true;
		label.AddThemeConstantOverride("outline_size", 1);
		return label;
	}

	private static bool IsRuneToggleInput(InputEvent inputEvent)
	{
		return inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }
			|| inputEvent is InputEventScreenTouch { Pressed: false };
	}

	private static Button CreateActionButton(string text, Action action)
	{
		Button button = new()
		{
			Text = text,
			CustomMinimumSize = new Vector2(132f, 38f),
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		button.AddThemeStyleboxOverride("normal", CreateButtonStyle(new Color(0.1f, 0.12f, 0.17f, 0.9f), new Color(0.46f, 0.55f, 0.68f, 0.78f)));
		button.AddThemeStyleboxOverride("hover", CreateButtonStyle(new Color(0.13f, 0.16f, 0.22f, 0.95f), new Color(0.88f, 0.72f, 0.36f, 0.92f)));
		button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(new Color(0.07f, 0.09f, 0.13f, 0.98f), new Color(0.88f, 0.62f, 0.28f, 0.92f)));
		button.AddThemeStyleboxOverride("focus", CreateButtonStyle(new Color(0.13f, 0.16f, 0.22f, 0.95f), new Color(0.88f, 0.72f, 0.36f, 0.92f)));
		button.AddThemeFontSizeOverride("font_size", 16);
		button.AddThemeColorOverride("font_color", new Color(0.96f, 0.94f, 0.88f, 1f));
		button.Pressed += action;
		return button;
	}

	private static void UpdateAllRuneIcons(IReadOnlyList<RuneIconBinding> bindings, IReadOnlySet<string> pendingDisabledIds)
	{
		foreach (RuneIconBinding binding in bindings)
		{
			ApplyRuneIconState(binding, !pendingDisabledIds.Contains(binding.Id));
		}
	}

	private static void ApplyRuneIconState(RuneIconBinding binding, bool enabled)
	{
		binding.Root.Modulate = enabled
			? Colors.White
			: new Color(0.42f, 0.44f, 0.48f, 0.5f);
	}

	private static void ToggleRune(string id, RuneIconBinding binding, HashSet<string> pendingDisabledIds, Label summary)
	{
		if (pendingDisabledIds.Contains(id))
		{
			pendingDisabledIds.Remove(id);
		}
		else
		{
			pendingDisabledIds.Add(id);
		}

		ApplyRuneIconState(binding, !pendingDisabledIds.Contains(id));
		UpdateSummary(summary, pendingDisabledIds);
	}

	private static void AttachRelicHoverTips(Control holder, RelicModel relic)
	{
		holder.MouseEntered += () => ShowRelicHoverTips(holder, relic);
		holder.MouseExited += () => NHoverTipSet.Remove(holder);
		holder.TreeExiting += () => NHoverTipSet.Remove(holder);
	}

	private static void ShowRelicHoverTips(Control holder, RelicModel relic)
	{
		NHoverTipSet.Remove(holder);
		NHoverTipSet? hoverTipSet = NHoverTipSet.CreateAndShow(holder, relic.HoverTips, HoverTip.GetHoverTipAlignment(holder));
		if (hoverTipSet == null)
		{
			return;
		}

		hoverTipSet.ZIndex = HoverTipZIndex;
		hoverTipSet.ZAsRelative = false;
		hoverTipSet.SetAlignment(holder, HoverTip.GetHoverTipAlignment(holder));
	}

	private static List<RuneConfigEntry> BuildRuneEntries()
	{
		List<RuneConfigEntry> entries = [];
		foreach (Type runeType in HextechCatalog.GetAllConfigurableRuneTypes())
		{
			RelicModel relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(runeType));
			ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
			HextechRarityTier rarity = GetRuneRarity(runeType);
			string rarityKey = rarity.ToString().ToUpperInvariant();
			string poolKey = HextechCatalog.GetPlayerRunePoolKey(relic);
			string tagKey = HextechCatalog.GetPlayerRuneTagKey(relic);
				entries.Add(new RuneConfigEntry(
					id.Entry,
					relic,
					relic.Title.GetFormattedText(),
					new LocString(LocTable, "HEXTECH_SERIES." + rarityKey).GetRawText(),
					new LocString(LocTable, "HEXTECH_POOL." + poolKey).GetRawText(),
				new LocString(LocTable, "HEXTECH_TAG." + tagKey).GetRawText(),
				(int)rarity,
				poolKey,
				tagKey));
		}

		return entries
			.OrderBy(static entry => entry.RarityOrder)
			.ThenBy(static entry => entry.PoolKey, StringComparer.Ordinal)
			.ThenBy(static entry => entry.TagKey, StringComparer.Ordinal)
			.ThenBy(static entry => entry.Title, StringComparer.CurrentCulture)
			.ToList();
	}

	private static HextechRarityTier GetRuneRarity(Type runeType)
	{
		if (HextechCatalog.GetConfigurablePlayerRuneTypesForRarity(HextechRarityTier.Silver).Contains(runeType))
		{
			return HextechRarityTier.Silver;
		}

		if (HextechCatalog.GetConfigurablePlayerRuneTypesForRarity(HextechRarityTier.Prismatic).Contains(runeType))
		{
			return HextechRarityTier.Prismatic;
		}

		return HextechRarityTier.Gold;
	}

	private static void UpdateSummary(Label summary, IReadOnlySet<string> pendingDisabledIds)
	{
		HashSet<string> configurableIds = HextechCatalog.GetConfigurablePlayerRuneIds()
			.Select(static id => id.Entry)
			.ToHashSet(StringComparer.Ordinal);
		int total = configurableIds.Count;
		int disabled = pendingDisabledIds.Count(configurableIds.Contains);
		int enabled = Math.Max(0, total - disabled);
		summary.Text = string.Format(L("HEXTECH_CONFIG_SUMMARY"), enabled, total);
	}

	private static Label CreateLabel(string text, int fontSize, Color color)
	{
		Label label = new()
		{
			Text = text,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.68f));
		label.AddThemeConstantOverride("outline_size", 2);
		return label;
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

	private static void RemoveExistingOverlay(Node root)
	{
		if (root.GetNodeOrNull<Control>(OverlayName) is { } overlay && GodotObject.IsInstanceValid(overlay))
		{
			overlay.QueueFree();
		}
	}

	private static Node ResolveRoot(Node node)
	{
		return node.GetTree()?.Root is Node root ? root : node;
	}

	private static TNode? FindAncestor<TNode>(Node node)
		where TNode : Node
	{
		Node? current = node;
		while (current != null)
		{
			if (current is TNode match)
			{
				return match;
			}

			current = current.GetParent();
		}

		return null;
	}

	private static async Task<bool> AwaitProcessFrameAsync(Node node)
	{
		if (!GodotObject.IsInstanceValid(node) || !node.IsInsideTree())
		{
			return false;
		}

		SceneTree tree = node.GetTree();
		if (tree == null)
		{
			return false;
		}

		await node.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		return GodotObject.IsInstanceValid(node) && node.IsInsideTree();
	}

	private static string L(string key)
	{
		try
		{
			return new LocString(LocTable, key).GetRawText();
		}
		catch
		{
			return key;
		}
	}

	private sealed record RuneConfigEntry(
		string Id,
		RelicModel Relic,
		string Title,
		string RarityText,
		string PoolText,
		string TagText,
		int RarityOrder,
		string PoolKey,
		string TagKey);

	private sealed record RuneIconBinding(
		string Id,
		Control Root);
}
