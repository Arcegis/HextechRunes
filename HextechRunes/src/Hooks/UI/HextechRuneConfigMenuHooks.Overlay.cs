using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace HextechRunes;

internal static partial class HextechRuneConfigMenuHooks
{
	private static void OpenOverlay(Node source)
	{
		Node root = ResolveRoot(source);
		RemoveExistingOverlay(root);
		HextechControllerOverlay overlay = CreateOverlay(out RuneConfigOverlayState state);
		if (source is Control opener)
		{
			overlay.SetMeta(OpenerMetaKey, opener);
		}

		root.AddChild(overlay);
		overlay.InitialFocus = state.InitialFocus;
		ConfigureHorizontalFocus(state.TabButtons);
		WireControllerFocusScrolling(overlay);
		TaskHelper.RunSafely(PopulateRuneIconsAsync(state.Context));
		TaskHelper.RunSafely(AnimateOverlayInAsync(overlay));
	}

	private static async Task AnimateOverlayInAsync(Control overlay)
	{
		if (!await HextechGodotAsync.AwaitProcessFrameAsync(overlay))
		{
			return;
		}

		overlay.Modulate = HextechUiTheme.TransparentWhite;
		Control? panel = overlay.GetNodeOrNull<Control>(ConfigPanelName);
		if (panel != null)
		{
			panel.PivotOffset = panel.Size * 0.5f;
			panel.Scale = Vector2.One * OverlayOpenScale;
		}

		Tween tween = overlay.CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(overlay, "modulate:a", 1f, OverlayOpenSeconds).SetEase(Tween.EaseType.Out);
		if (panel != null)
		{
			tween.TweenProperty(panel, "scale", Vector2.One, OverlayOpenSeconds)
				.SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Back);
		}
	}

	private static void CloseWithoutSaving(Control overlay)
	{
		if (!GodotObject.IsInstanceValid(overlay))
		{
			return;
		}

		overlay.GetViewport()?.SetInputAsHandled();
		CloseOverlayAnimated(overlay);
	}

	private static void CloseOverlayAnimated(Control overlay)
	{
		if (!GodotObject.IsInstanceValid(overlay))
		{
			return;
		}

		// 防止「保存」与「取消」连点触发两次关闭动画。
		if (overlay.HasMeta(ClosingMetaKey))
		{
			return;
		}

		overlay.SetMeta(ClosingMetaKey, true);
		overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
		(overlay as HextechControllerOverlay)?.ReleaseHostFocusBlock();
		Control? panel = overlay.GetNodeOrNull<Control>(ConfigPanelName);
		if (panel != null)
		{
			panel.PivotOffset = panel.Size * 0.5f;
		}

		Tween tween = overlay.CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(overlay, "modulate:a", 0f, OverlayCloseSeconds).SetEase(Tween.EaseType.In);
		if (panel != null)
		{
			tween.TweenProperty(panel, "scale", Vector2.One * OverlayOpenScale, OverlayCloseSeconds).SetEase(Tween.EaseType.In);
		}

		Control? opener = overlay.HasMeta(OpenerMetaKey) ? overlay.GetMeta(OpenerMetaKey).As<Control>() : null;
		tween.Chain().TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(overlay))
			{
				overlay.QueueFree();
			}

			// 覆盖层不是原版 SubmenuStack 的一员,焦点要自己还给打开它的按钮;鼠标玩家不抢焦点,免得主菜单按钮停在焦点态。
			if (HextechControllerInput.IsDirectionalNavigation
				&& opener != null && GodotObject.IsInstanceValid(opener) && opener.IsInsideTree() && opener.IsVisibleInTree())
			{
				opener.GrabFocus();
			}
		}));
	}

	private static HextechControllerOverlay CreateOverlay(out RuneConfigOverlayState state)
	{
		bool compactLayout = IsCompactConfigLayout();
		HextechControllerOverlay overlay = CreateOverlayRoot(compactLayout, out VBoxContainer content);

		Label title = CreateLabel(L("HEXTECH_CONFIG_TITLE"), compactLayout ? 26 : 30, new Color(0.98f, 0.94f, 0.82f, 1f));
		title.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(title);

		PendingConfig pending = PendingConfig.From(
			HextechRuneConfiguration.GetSnapshot(),
			HextechUiPreferences.ShowHiddenRelicsToggle,
			HextechUiPreferences.ShowUpdateNotice,
			HextechUiPreferences.CollapseEnemyHexes,
			HextechUiPreferences.ConfirmRuneSelection);
		List<RuneConfigEntry> playerEntries = BuildRuneEntries();
		List<RuneConfigEntry> enemyEntries = BuildEnemyHexEntries();
		List<RuneConfigEntry> forgeEntries = BuildForgeEntries();
		Label summary = CreateLabel(string.Empty, compactLayout ? 15 : 16, new Color(0.92f, 0.88f, 0.7f, 0.95f));
		ConfigMenuContext context = new(
			overlay,
			pending,
			playerEntries,
			enemyEntries,
			forgeEntries,
			ConfigPoolIds.FromEntries(playerEntries, enemyEntries, forgeEntries),
			summary,
			compactLayout);

		Label description = CreateLabel(string.Empty, compactLayout ? 13 : 15, new Color(0.82f, 0.86f, 0.92f, 0.92f));
		description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		description.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(description);

		Control[] pages =
		[
			CreateSelectionPage(context),
			CreateRunePoolPage(context),
			CreateIconPoolPage(context, context.ForgeEntries, context.Pending.DisabledForgeIds, context.ForgeIconBindings, L("HEXTECH_CONFIG_TAB_FORGES")),
			CreateDetailsPage(context)
		];

		List<Button> tabButtons = [];
		Action<ConfigPage>? updatePageActions = null;
		ConfigPage? previousPage = null;
		void SelectPage(ConfigPage page)
		{
			bool changed = page != previousPage;
			previousPage = page;
			context.SelectedPage = page;
			for (int i = 0; i < pages.Length; i++)
			{
				pages[i].Visible = i == (int)page;
			}

			if (changed)
			{
				AnimatePageIn(pages[(int)page]);
			}

			UpdateTabButtonStates(tabButtons, (int)page, compactLayout);
			AnimateTabIndicator(tabButtons, (int)page, changed);
			UpdatePageDescription(description, page);
			updatePageActions?.Invoke(page);
			context.UpdateSummary();
		}

		content.AddChild(CreateTabBar(tabButtons, SelectPage, compactLayout));
		overlay.CycleTab = delta => SelectPage((ConfigPage)((((int)context.SelectedPage + delta) % pages.Length + pages.Length) % pages.Length));

		VBoxContainer pageHost = CreatePageHost(content, compactLayout);
		content.AddChild(CreateBottomBar(context, out updatePageActions));
		foreach (Control page in pages)
		{
			page.Visible = false;
			pageHost.AddChild(page);
		}

		SelectPage(ConfigPage.Counts);
		context.UpdateSummary();
		state = new RuneConfigOverlayState(context, tabButtons[0], tabButtons);
		return overlay;
	}

	/// <summary>覆盖层骨架:全屏遮罩 + 居中面板 + 内容列;返回覆盖层,内容列从 <paramref name="content"/> 取。</summary>
	private static HextechControllerOverlay CreateOverlayRoot(bool compactLayout, out VBoxContainer content)
	{
		HextechControllerOverlay overlay = new()
		{
			Name = OverlayName,
			MouseFilter = Control.MouseFilterEnum.Stop,
			FocusMode = Control.FocusModeEnum.All,
			FocusBehaviorRecursive = Control.FocusBehaviorRecursiveEnum.Enabled,
			ZIndex = OverlayZIndex
		};
		overlay.CancelRequested = () => CloseWithoutSaving(overlay);
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
			Name = ConfigPanelName,
			CustomMinimumSize = GetResponsivePanelSize(),
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());
		center.AddChild(panel);

		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", compactLayout ? 20 : 28);
		margin.AddThemeConstantOverride("margin_right", compactLayout ? 20 : 28);
		margin.AddThemeConstantOverride("margin_top", compactLayout ? 16 : 24);
		margin.AddThemeConstantOverride("margin_bottom", compactLayout ? 16 : 24);
		panel.AddChild(margin);

		content = new VBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		content.AddThemeConstantOverride("separation", compactLayout ? 8 : 14);
		margin.AddChild(content);
		return overlay;
	}

	private static void UpdatePageDescription(Label description, ConfigPage page)
	{
		string text = page switch
		{
			ConfigPage.Counts => L(IsEnemyHexCountConfigReadOnly() ? "HEXTECH_CONFIG_CLIENT_READONLY" : "HEXTECH_CONFIG_DESCRIPTION"),
			ConfigPage.RunePools or ConfigPage.Forges => L("HEXTECH_CONFIG_POOL_HINT"),
			ConfigPage.Details => L("HEXTECH_CONFIG_MISC_HINT"),
			_ => string.Empty
		};
		SetLabelText(description, text);
		description.Visible = text.Length > 0;
	}

	private static bool IsEnemyHexCountConfigReadOnly()
	{
		return HextechPlayerContextHelper.IsClientRun();
	}

	/// <summary>页签条:四个页签按钮 + 滑动的金色下划线。</summary>
	private static Control CreateTabBar(List<Button> tabButtons, Action<ConfigPage> selectPage, bool compactLayout)
	{
		PanelContainer tabShell = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		tabShell.AddThemeStyleboxOverride("panel", CreateTabShellStyle());

		// 下划线叠在页签行上方,不作为 HBox 的一格参与排版,所以另套一层普通 Control。
		Control tabHolder = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		tabShell.AddChild(tabHolder);

		HBoxContainer tabs = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		tabs.AddThemeConstantOverride("separation", 0);
		tabs.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		tabHolder.AddChild(tabs);

		// 在页签之后添加,金色下划线才画在激活页签的高亮底色之上。
		ColorRect tabIndicator = new()
		{
			Name = TabIndicatorName,
			Color = new Color(0.96f, 0.78f, 0.38f, 0.98f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		tabHolder.AddChild(tabIndicator);

		(ConfigPage Page, string LocKey)[] tabDefinitions =
		[
			(ConfigPage.Counts, "HEXTECH_CONFIG_TAB_COUNTS"),
			(ConfigPage.RunePools, "HEXTECH_CONFIG_TAB_RUNE_POOLS"),
			(ConfigPage.Forges, "HEXTECH_CONFIG_TAB_FORGES"),
			(ConfigPage.Details, "HEXTECH_CONFIG_TAB_DETAILS")
		];
		Vector2 tabButtonSize = GetTabButtonSize(compactLayout);
		tabHolder.CustomMinimumSize = new Vector2(tabButtonSize.X * tabDefinitions.Length, tabButtonSize.Y);
		tabIndicator.Size = new Vector2(tabButtonSize.X, 3f);
		tabIndicator.Position = new Vector2(0f, tabButtonSize.Y - 3f);
		foreach ((ConfigPage page, string locKey) in tabDefinitions)
		{
			Button button = CreateTabButton(L(locKey), () => selectPage(page), compactLayout);
			tabButtons.Add(button);
			tabs.AddChild(button);
		}

		return tabShell;
	}

	/// <summary>页面滚动区:内宽钉在最宽的符文网格上,切页签时面板边框不跳动。</summary>
	private static VBoxContainer CreatePageHost(VBoxContainer content, bool compactLayout)
	{
		ScrollContainer scroll = new()
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		VBoxContainer pages = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(GetRuneGridMinWidth(compactLayout), 0f)
		};
		pages.AddThemeConstantOverride("separation", compactLayout ? 12 : 16);
		scroll.AddChild(pages);
		content.AddChild(scroll);
		return pages;
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
}
