using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
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
	private const int RuneConfigColumns = 8;
	private const string BaseConfigSourceKey = "0:HextechRunes";
	private const string ExternalConfigSourcePrefix = "1:";
	private const string SponsorPackModId = "HextechRunesSponsorPack";
	private const float ConfigRuneHolderScale = 1.3f;
	private const float RuneConfigCellWidth = 108f;
	private const float RuneConfigCellHeight = 136f;
	private const float RuneConfigIconLayerHeight = 96f;
	private const float RuneConfigDragThreshold = 12f;
	private const float RuneConfigLongPressSeconds = 0.35f;
	private const float StepRepeatInitialDelaySeconds = 0.35f;
	private const float StepRepeatIntervalSeconds = 0.075f;
	private const float StepRepeatFastIntervalSeconds = 0.035f;
	private const int StepRepeatFastAfterTicks = 10;
	private const int RuneConfigIconsPerFrame = 12;
	private const float CompactConfigHeightThreshold = 820f;
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
		if (host.FindChild(ButtonName, recursive: true, owned: false) is NMainMenuTextButton existingNative
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

		if (mainMenu.GetNodeOrNull<Control>("MainMenuTextButtons") is not { } buttonHost
			|| mainMenu.GetNodeOrNull<NMainMenuTextButton>("MainMenuTextButtons/SettingsButton") is not { } settingsButton)
		{
			return false;
		}

		NMainMenuTextButton configButton = (NMainMenuTextButton)((Node)settingsButton).Duplicate(NativeDuplicateFlags);
		((Node)configButton).Name = ButtonName;
		((Node)configButton).UniqueNameInOwner = true;
		buttonHost.AddChild(configButton);
		buttonHost.MoveChild(configButton, Math.Min(settingsButton.GetIndex() + 1, buttonHost.GetChildCount() - 1));
		ConfigureNativeMenuLabel(configButton);
		ConfigureNativeMenuButton(configButton, settingsButton);
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
			label.PivotOffset = label.Size * 0.5f;
		}

		((Control)configButton).TooltipText = L("HEXTECH_CONFIG_BUTTON_TOOLTIP");
	}

	private static void ConfigureNativeMenuButton(NMainMenuTextButton configButton, NMainMenuTextButton template)
	{
		Control control = configButton;
		control.MouseFilter = Control.MouseFilterEnum.Stop;
		control.FocusMode = Control.FocusModeEnum.All;
		control.MouseDefaultCursorShape = ((Control)template).MouseDefaultCursorShape;
		control.SizeFlagsHorizontal = template.SizeFlagsHorizontal;
		control.SizeFlagsVertical = template.SizeFlagsVertical;
		control.CustomMinimumSize = template.CustomMinimumSize;
		control.ZIndex = ((Control)template).ZIndex;
		control.ZAsRelative = ((Control)template).ZAsRelative;
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
		Control overlay = CreateOverlay(out RuneConfigOverlayState state);
		root.AddChild(overlay);
		_ = PopulateRuneIconsAsync(overlay, state);
	}

	private static Control CreateOverlay(out RuneConfigOverlayState state)
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

		bool compactLayout = IsCompactConfigLayout();
		PanelContainer panel = new()
		{
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

		VBoxContainer content = new()
		{
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		content.AddThemeConstantOverride("separation", compactLayout ? 8 : 14);
		margin.AddChild(content);

		Label title = CreateLabel(L("HEXTECH_CONFIG_TITLE"), compactLayout ? 26 : 30, new Color(0.98f, 0.94f, 0.82f, 1f));
		title.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(title);

		HextechRunConfigurationSnapshot pendingSnapshot = HextechRuneConfiguration.GetSnapshot();
		int[] pendingPlayerHexCounts = pendingSnapshot.PlayerHexCountsByAct.ToArray();
		int[] pendingEnemyHexCounts = pendingSnapshot.EnemyHexCountsByAct.ToArray();
		int[] pendingPlayerRuneRerollLimit = [ pendingSnapshot.PlayerRuneRerollLimit ];
		int[] pendingMonsterHexRerollLimit = [ pendingSnapshot.MonsterHexRerollLimit ];
		HashSet<string> pendingDisabledPlayerIds = pendingSnapshot.DisabledPlayerRuneIds.ToHashSet(StringComparer.Ordinal);
		HashSet<string> pendingDisabledMonsterHexIds = pendingSnapshot.DisabledMonsterHexIds.ToHashSet(StringComparer.Ordinal);
		HashSet<string> pendingDisabledForgeIds = pendingSnapshot.DisabledForgeIds.ToHashSet(StringComparer.Ordinal);
		int[] pendingFirstActRuneWeights = ToWeightArray(pendingSnapshot.FirstActRuneRarityWeights);
		int[] pendingNormalRuneWeights = ToWeightArray(pendingSnapshot.NormalRuneRarityWeights);
		int[] pendingSecondActAfterSilverWeights = ToWeightArray(pendingSnapshot.SecondActAfterSilverRuneRarityWeights);
		int[] pendingForgeWeights = ToWeightArray(pendingSnapshot.ForgeRarityWeights);
		int[] pendingForgePrice = [ pendingSnapshot.RandomForgeShopPrice ];
		bool[] pendingShowHiddenRelicsToggle = [ HextechRelicVisibilityHooks.GetShowHiddenRelicsToggle() ];
		List<NumericValueBinding> numericBindings = [];
		List<BooleanValueBinding> booleanBindings = [];
		bool configReadOnly = IsEnemyHexCountConfigReadOnly();

		Label description = CreateLabel(L(configReadOnly ? "HEXTECH_CONFIG_CLIENT_READONLY" : "HEXTECH_CONFIG_DESCRIPTION"), compactLayout ? 13 : 16, new Color(0.82f, 0.86f, 0.92f, 0.92f));
		description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		content.AddChild(description);

		List<RuneConfigEntry> playerEntries = BuildRuneEntries();
		List<RuneConfigEntry> enemyEntries = BuildEnemyHexEntries();
		List<RuneConfigEntry> forgeEntries = BuildForgeEntries();
		List<RuneIconBinding> playerIconBindings = [];
		List<RuneIconBinding> enemyIconBindings = [];
		List<RuneIconBinding> forgeIconBindings = [];
		List<RuneConfigLoadTarget> loadTargets = [];
		int selectedPageIndex = 0;
		Label summary = CreateLabel(string.Empty, compactLayout ? 15 : 16, new Color(0.92f, 0.88f, 0.7f, 0.95f));
		Action updateSummary = () => UpdateSummary(summary, selectedPageIndex, pendingDisabledPlayerIds, pendingDisabledMonsterHexIds, pendingDisabledForgeIds);

		Control countsPage = CreateSelectionPage(pendingPlayerHexCounts, pendingEnemyHexCounts, pendingPlayerRuneRerollLimit, pendingMonsterHexRerollLimit, numericBindings, compactLayout);
		Control runePoolPage = CreateRunePoolPage(playerEntries, pendingDisabledPlayerIds, enemyEntries, pendingDisabledMonsterHexIds, loadTargets, compactLayout);
		Control forgePoolPage = CreateIconPoolPage(forgeEntries, pendingDisabledForgeIds, loadTargets, L("HEXTECH_CONFIG_TAB_FORGES"), compactLayout);
		Control detailsPage = CreateDetailsPage(
			pendingFirstActRuneWeights,
			pendingNormalRuneWeights,
			pendingSecondActAfterSilverWeights,
			pendingForgeWeights,
			pendingForgePrice,
			pendingShowHiddenRelicsToggle,
			numericBindings,
			booleanBindings,
			compactLayout);
		Control[] pageArray = [ countsPage, runePoolPage, forgePoolPage, detailsPage ];

		HBoxContainer tabs = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		tabs.AddThemeConstantOverride("separation", compactLayout ? 6 : 10);
		content.AddChild(tabs);

		List<Button> tabButtons = [];
		Action<int>? updatePageActions = null;
		Action<int> selectPage = pageIndex =>
		{
			selectedPageIndex = pageIndex;
			for (int i = 0; i < pageArray.Length; i++)
			{
				pageArray[i].Visible = i == pageIndex;
			}

			UpdateTabButtonStates(tabButtons, pageIndex, compactLayout);
			updatePageActions?.Invoke(pageIndex);
			updateSummary();
		};
		AddConfigTab(tabs, tabButtons, L("HEXTECH_CONFIG_TAB_COUNTS"), () => selectPage(0), compactLayout);
		AddConfigTab(tabs, tabButtons, L("HEXTECH_CONFIG_TAB_RUNE_POOLS"), () => selectPage(1), compactLayout);
		AddConfigTab(tabs, tabButtons, L("HEXTECH_CONFIG_TAB_FORGES"), () => selectPage(2), compactLayout);
		AddConfigTab(tabs, tabButtons, L("HEXTECH_CONFIG_TAB_DETAILS"), () => selectPage(3), compactLayout);

		content.AddChild(CreateConfigPageActions(
			playerEntries,
			enemyEntries,
			forgeEntries,
			pendingDisabledPlayerIds,
			pendingDisabledMonsterHexIds,
			pendingDisabledForgeIds,
			pendingPlayerHexCounts,
			pendingEnemyHexCounts,
			pendingPlayerRuneRerollLimit,
			pendingMonsterHexRerollLimit,
			pendingFirstActRuneWeights,
			pendingNormalRuneWeights,
			pendingSecondActAfterSilverWeights,
			pendingForgeWeights,
			pendingForgePrice,
			pendingShowHiddenRelicsToggle,
			numericBindings,
			booleanBindings,
			playerIconBindings,
			enemyIconBindings,
			forgeIconBindings,
			updateSummary,
			() => selectedPageIndex,
			compactLayout,
			out updatePageActions));
		content.AddChild(summary);

		ScrollContainer scroll = new()
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		VBoxContainer pages = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		pages.AddThemeConstantOverride("separation", compactLayout ? 12 : 16);
		scroll.AddChild(pages);
		content.AddChild(scroll);

		content.AddChild(CreateConfigFooter(
			overlay,
			pendingPlayerHexCounts,
			pendingEnemyHexCounts,
			pendingDisabledPlayerIds,
			pendingDisabledMonsterHexIds,
			pendingDisabledForgeIds,
			pendingFirstActRuneWeights,
			pendingNormalRuneWeights,
			pendingSecondActAfterSilverWeights,
			pendingForgeWeights,
			pendingForgePrice,
			pendingPlayerRuneRerollLimit,
			pendingMonsterHexRerollLimit,
			pendingShowHiddenRelicsToggle,
			compactLayout));

		foreach (Control page in pageArray)
		{
			page.Visible = false;
			pages.AddChild(page);
		}

		selectPage(0);
		updateSummary();
		state = new RuneConfigOverlayState(
			loadTargets,
			pendingDisabledPlayerIds,
			pendingDisabledMonsterHexIds,
			pendingDisabledForgeIds,
			playerIconBindings,
			enemyIconBindings,
			forgeIconBindings,
			updateSummary);
		return overlay;
	}

	private static int[] ToWeightArray(HextechRarityWeights weights)
	{
		return [ weights.Silver, weights.Gold, weights.Prismatic ];
	}

	private static int[] ToWeightArray(HextechForgeRarityWeights weights)
	{
		return [ weights.Silver, weights.Gold, weights.Prismatic ];
	}

	private static HextechRarityWeights ToRarityWeights(IReadOnlyList<int> weights)
	{
		return new HextechRarityWeights(
			weights.Count > 0 ? weights[0] : 0,
			weights.Count > 1 ? weights[1] : 0,
			weights.Count > 2 ? weights[2] : 0);
	}

	private static HextechForgeRarityWeights ToForgeRarityWeights(IReadOnlyList<int> weights)
	{
		return new HextechForgeRarityWeights(
			weights.Count > 0 ? weights[0] : 0,
			weights.Count > 1 ? weights[1] : 0,
			weights.Count > 2 ? weights[2] : 0);
	}

	private static Control CreateSelectionPage(
		int[] pendingPlayerHexCounts,
		int[] pendingEnemyHexCounts,
		int[] pendingPlayerRuneRerollLimit,
		int[] pendingMonsterHexRerollLimit,
		List<NumericValueBinding> numericBindings,
		bool compactLayout)
	{
		VBoxContainer page = CreatePageContainer(compactLayout);
		page.AddChild(CreateActCountSection(
			L("HEXTECH_PLAYER_COUNT_TITLE"),
			L("HEXTECH_PLAYER_COUNT_DESCRIPTION"),
			pendingPlayerHexCounts,
			HextechRuneConfiguration.ClampPlayerHexCount,
			numericBindings,
			compactLayout));
		page.AddChild(CreateActCountSection(
			L("HEXTECH_ENEMY_COUNT_TITLE"),
			L("HEXTECH_ENEMY_COUNT_DESCRIPTION"),
			pendingEnemyHexCounts,
			HextechRuneConfiguration.ClampEnemyHexCount,
			numericBindings,
			compactLayout));
		page.AddChild(CreateRerollLimitSection(
			pendingPlayerRuneRerollLimit,
			pendingMonsterHexRerollLimit,
			numericBindings,
			compactLayout));
		return page;
	}

	private static VBoxContainer CreatePageContainer(bool compactLayout)
	{
		VBoxContainer page = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		page.AddThemeConstantOverride("separation", compactLayout ? 12 : 16);
		return page;
	}

	private static Control CreateActCountSection(
		string titleText,
		string descriptionText,
		int[] counts,
		Func<int, int> clamp,
		List<NumericValueBinding> numericBindings,
		bool compactLayout)
	{
		VBoxContainer section = CreatePageContainer(compactLayout);
		section.AddChild(CreateSectionHeader(titleText, compactLayout ? 18 : 20));
		Label description = CreateLabel(descriptionText, compactLayout ? 13 : 14, new Color(0.78f, 0.84f, 0.9f, 0.9f));
		description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		section.AddChild(description);

		HBoxContainer row = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		row.AddThemeConstantOverride("separation", compactLayout ? 10 : 18);
		section.AddChild(row);
		row.AddChild(CreateNumericStepper(L("HEXTECH_ENEMY_COUNT_ACT1"), () => counts[0], value => counts[0] = clamp(value), numericBindings, compactLayout));
		row.AddChild(CreateNumericStepper(L("HEXTECH_ENEMY_COUNT_ACT2"), () => counts[1], value => counts[1] = clamp(value), numericBindings, compactLayout));
		row.AddChild(CreateNumericStepper(L("HEXTECH_ENEMY_COUNT_ACT3"), () => counts[2], value => counts[2] = clamp(value), numericBindings, compactLayout));
		return section;
	}

	private static Control CreateRerollLimitSection(
		int[] pendingPlayerRuneRerollLimit,
		int[] pendingMonsterHexRerollLimit,
		List<NumericValueBinding> numericBindings,
		bool compactLayout)
	{
		VBoxContainer section = CreatePageContainer(compactLayout);
		section.AddChild(CreateSectionHeader(L("HEXTECH_REROLL_LIMIT_TITLE"), compactLayout ? 18 : 20));
		Label description = CreateLabel(L("HEXTECH_REROLL_LIMIT_DESCRIPTION"), compactLayout ? 13 : 14, new Color(0.78f, 0.84f, 0.9f, 0.9f));
		description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		section.AddChild(description);

		HBoxContainer row = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		row.AddThemeConstantOverride("separation", compactLayout ? 10 : 18);
		section.AddChild(row);
		row.AddChild(CreateRerollLimitStepper(
			L("HEXTECH_PLAYER_REROLL_LIMIT_LABEL"),
			() => pendingPlayerRuneRerollLimit[0],
			value => pendingPlayerRuneRerollLimit[0] = HextechRuneConfiguration.ClampRerollLimit(value),
			numericBindings,
			compactLayout));
		row.AddChild(CreateRerollLimitStepper(
			L("HEXTECH_MONSTER_REROLL_LIMIT_LABEL"),
			() => pendingMonsterHexRerollLimit[0],
			value => pendingMonsterHexRerollLimit[0] = HextechRuneConfiguration.ClampRerollLimit(value),
			numericBindings,
			compactLayout));
		return section;
	}

	private static Control CreateRunePoolPage(
		IReadOnlyList<RuneConfigEntry> playerEntries,
		HashSet<string> pendingDisabledPlayerIds,
		IReadOnlyList<RuneConfigEntry> enemyEntries,
		HashSet<string> pendingDisabledMonsterHexIds,
		List<RuneConfigLoadTarget> loadTargets,
		bool compactLayout)
	{
		VBoxContainer page = CreatePageContainer(compactLayout);
		page.AddChild(CreateSectionHeader(L("HEXTECH_PLAYER_POOL_TITLE"), compactLayout ? 18 : 20));
		AddIconPoolEntries(page, playerEntries, pendingDisabledPlayerIds, loadTargets, compactLayout);
		page.AddChild(CreateSectionHeader(L("HEXTECH_ENEMY_POOL_TITLE"), compactLayout ? 18 : 20));
		AddIconPoolEntries(page, enemyEntries, pendingDisabledMonsterHexIds, loadTargets, compactLayout);
		return page;
	}

	private static Control CreateIconPoolPage(
		IReadOnlyList<RuneConfigEntry> entries,
		HashSet<string> pendingDisabledIds,
		List<RuneConfigLoadTarget> loadTargets,
		string title,
		bool compactLayout)
	{
		VBoxContainer page = CreatePageContainer(compactLayout);
		page.AddChild(CreateSectionHeader(title, compactLayout ? 18 : 20));
		AddIconPoolEntries(page, entries, pendingDisabledIds, loadTargets, compactLayout);
		return page;
	}

	private static void AddIconPoolEntries(
		VBoxContainer page,
		IReadOnlyList<RuneConfigEntry> entries,
		HashSet<string> pendingDisabledIds,
		List<RuneConfigLoadTarget> loadTargets,
		bool compactLayout)
	{
		foreach (IGrouping<int, RuneConfigEntry> rarityGroup in entries.GroupBy(static entry => entry.RarityOrder))
		{
			page.AddChild(CreateSectionHeader(rarityGroup.First().RarityText, compactLayout ? 16 : 18));
			List<IGrouping<string, RuneConfigEntry>> sourceGroups = rarityGroup
				.GroupBy(static entry => entry.SourceKey)
				.ToList();
			foreach (IGrouping<string, RuneConfigEntry> sourceGroup in sourceGroups)
			{
				if (sourceGroups.Count > 1)
				{
					page.AddChild(CreateSourceHeader(sourceGroup.First().SourceText, compactLayout));
				}

				VBoxContainer grid = CreateRuneGrid(compactLayout);
				page.AddChild(grid);

				HBoxContainer? currentRow = null;
				int column = 0;
				foreach (RuneConfigEntry entry in sourceGroup)
				{
					if (column == 0)
					{
						currentRow = CreateRuneRow(compactLayout);
						grid.AddChild(currentRow);
					}

					CenterContainer slot = CreateRuneSlot();
					currentRow?.AddChild(slot);
					loadTargets.Add(new RuneConfigLoadTarget(entry, slot, pendingDisabledIds));

					column++;
					if (column == RuneConfigColumns)
					{
						column = 0;
					}
				}

				if (currentRow != null && column > 0)
				{
					for (; column < RuneConfigColumns; column++)
					{
						currentRow.AddChild(CreateRuneSlot());
					}
				}
			}
		}
	}

	private static Control CreateDetailsPage(
		int[] pendingFirstActRuneWeights,
		int[] pendingNormalRuneWeights,
		int[] pendingSecondActAfterSilverWeights,
		int[] pendingForgeWeights,
		int[] pendingForgePrice,
		bool[] pendingShowHiddenRelicsToggle,
		List<NumericValueBinding> numericBindings,
		List<BooleanValueBinding> booleanBindings,
		bool compactLayout)
	{
		VBoxContainer page = CreatePageContainer(compactLayout);
		page.AddChild(CreateMiscUiSection(pendingShowHiddenRelicsToggle, booleanBindings, compactLayout));
		page.AddChild(CreatePriceSection(pendingForgePrice, numericBindings, compactLayout));
		page.AddChild(CreateWeightSection(L("HEXTECH_FIRST_ACT_RUNE_RARITY_TITLE"), pendingFirstActRuneWeights, numericBindings, compactLayout));
		page.AddChild(CreateWeightSection(L("HEXTECH_NORMAL_RUNE_RARITY_TITLE"), pendingNormalRuneWeights, numericBindings, compactLayout));
		page.AddChild(CreateWeightSection(L("HEXTECH_AFTER_SILVER_RUNE_RARITY_TITLE"), pendingSecondActAfterSilverWeights, numericBindings, compactLayout));
		page.AddChild(CreateWeightSection(L("HEXTECH_FORGE_RARITY_TITLE"), pendingForgeWeights, numericBindings, compactLayout));
		return page;
	}

	private static Control CreateMiscUiSection(bool[] pendingShowHiddenRelicsToggle, List<BooleanValueBinding> booleanBindings, bool compactLayout)
	{
		VBoxContainer section = CreatePageContainer(compactLayout);
		section.AddChild(CreateSectionHeader(L("HEXTECH_MISC_UI_TITLE"), compactLayout ? 18 : 20));
		section.AddChild(CreateBooleanOption(
			L("HEXTECH_SHOW_HIDDEN_RELICS_TOGGLE_TITLE"),
			L("HEXTECH_SHOW_HIDDEN_RELICS_TOGGLE_DESCRIPTION"),
			() => pendingShowHiddenRelicsToggle[0],
			value => pendingShowHiddenRelicsToggle[0] = value,
			booleanBindings,
			compactLayout));
		return section;
	}

	private static Control CreateBooleanOption(
		string titleText,
		string descriptionText,
		Func<bool> getValue,
		Action<bool> setValue,
		List<BooleanValueBinding> booleanBindings,
		bool compactLayout)
	{
		HBoxContainer row = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		row.AddThemeConstantOverride("separation", compactLayout ? 8 : 12);

		CheckBox checkBox = new()
		{
			Text = string.Empty,
			ButtonPressed = getValue(),
			CustomMinimumSize = compactLayout ? new Vector2(36f, 36f) : new Vector2(42f, 42f),
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			FocusMode = Control.FocusModeEnum.All
		};
		checkBox.Toggled += value => setValue(value);
		booleanBindings.Add(new BooleanValueBinding(getValue, checkBox));
		row.AddChild(checkBox);

		VBoxContainer textColumn = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		textColumn.AddThemeConstantOverride("separation", compactLayout ? 2 : 4);
		Label title = CreateLabel(titleText, compactLayout ? 14 : 16, new Color(0.96f, 0.92f, 0.78f, 0.98f));
		title.HorizontalAlignment = HorizontalAlignment.Left;
		textColumn.AddChild(title);
		Label description = CreateLabel(descriptionText, compactLayout ? 12 : 13, new Color(0.78f, 0.84f, 0.9f, 0.88f));
		description.HorizontalAlignment = HorizontalAlignment.Left;
		description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		textColumn.AddChild(description);
		row.AddChild(textColumn);

		row.GuiInput += inputEvent =>
		{
			if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
			{
				checkBox.ButtonPressed = !checkBox.ButtonPressed;
				row.GetViewport()?.SetInputAsHandled();
			}
		};
		return row;
	}

	private static Control CreatePriceSection(int[] price, List<NumericValueBinding> numericBindings, bool compactLayout)
	{
		VBoxContainer section = CreatePageContainer(compactLayout);
		section.AddChild(CreateSectionHeader(L("HEXTECH_FORGE_PRICE_TITLE"), compactLayout ? 18 : 20));
		HBoxContainer row = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		row.AddChild(CreateNumericStepper(
			L("HEXTECH_FORGE_PRICE_LABEL"),
			() => price[0],
			value => price[0] = HextechRuneConfiguration.ClampRandomForgeShopPrice(value),
			numericBindings,
			compactLayout,
			step: 10));
		section.AddChild(row);
		return section;
	}

	private static Control CreateWeightSection(string title, int[] weights, List<NumericValueBinding> numericBindings, bool compactLayout)
	{
		VBoxContainer section = CreatePageContainer(compactLayout);
		section.AddChild(CreateSectionHeader(title, compactLayout ? 18 : 20));
		HBoxContainer row = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		row.AddThemeConstantOverride("separation", compactLayout ? 10 : 18);
		section.AddChild(row);
		row.AddChild(CreateNumericStepper(L("HEXTECH_RARITY_SILVER"), () => weights[0], value => weights[0] = HextechRuneConfiguration.ClampRarityWeight(value), numericBindings, compactLayout));
		row.AddChild(CreateNumericStepper(L("HEXTECH_RARITY_GOLD"), () => weights[1], value => weights[1] = HextechRuneConfiguration.ClampRarityWeight(value), numericBindings, compactLayout));
		row.AddChild(CreateNumericStepper(L("HEXTECH_RARITY_PRISMATIC"), () => weights[2], value => weights[2] = HextechRuneConfiguration.ClampRarityWeight(value), numericBindings, compactLayout));
		return section;
	}

	private static Control CreateNumericStepper(
		string labelText,
		Func<int> getValue,
		Action<int> setValue,
		List<NumericValueBinding> numericBindings,
		bool compactLayout,
		int step = 1,
		Func<string>? getDisplayText = null,
		Func<int, int, int>? stepValue = null)
	{
		VBoxContainer root = new()
		{
			CustomMinimumSize = compactLayout ? new Vector2(150f, 58f) : new Vector2(190f, 70f),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		root.AddThemeConstantOverride("separation", compactLayout ? 3 : 5);

		Label label = CreateLabel(labelText, compactLayout ? 13 : 15, new Color(0.92f, 0.9f, 0.78f, 0.96f));
		label.HorizontalAlignment = HorizontalAlignment.Center;
		root.AddChild(label);

		HBoxContainer controls = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		controls.AddThemeConstantOverride("separation", compactLayout ? 6 : 8);
		root.AddChild(controls);

		string GetDisplay() => getDisplayText?.Invoke() ?? getValue().ToString();
		Label number = CreateLabel(GetDisplay(), compactLayout ? 17 : 18, new Color(0.98f, 0.98f, 0.94f, 1f));
		number.HorizontalAlignment = HorizontalAlignment.Center;
		number.VerticalAlignment = VerticalAlignment.Center;
		number.CustomMinimumSize = compactLayout ? new Vector2(44f, 32f) : new Vector2(54f, 34f);
		numericBindings.Add(new NumericValueBinding(GetDisplay, number));

		Button minus = CreateStepButton("-", false, compactLayout);
		Button plus = CreateStepButton("+", false, compactLayout);
		AttachRepeatingStep(minus, () =>
		{
			setValue(stepValue?.Invoke(getValue(), -step) ?? getValue() - step);
			SetLabelText(number, GetDisplay());
		});
		AttachRepeatingStep(plus, () =>
		{
			setValue(stepValue?.Invoke(getValue(), step) ?? getValue() + step);
			SetLabelText(number, GetDisplay());
		});

		controls.AddChild(minus);
		controls.AddChild(number);
		controls.AddChild(plus);
		return root;
	}

	private static Control CreateRerollLimitStepper(
		string labelText,
		Func<int> getValue,
		Action<int> setValue,
		List<NumericValueBinding> numericBindings,
		bool compactLayout)
	{
		return CreateNumericStepper(
			labelText,
			getValue,
			setValue,
			numericBindings,
			compactLayout,
			getDisplayText: () => FormatRerollLimit(getValue()),
			stepValue: static (current, delta) => HextechRuneConfiguration.StepRerollLimit(current, delta));
	}

	private static string FormatRerollLimit(int value)
	{
		return HextechRuneConfiguration.ClampRerollLimit(value) == HextechRuneConfiguration.InfiniteRerollLimit
			? L("HEXTECH_REROLL_LIMIT_INFINITE")
			: HextechRuneConfiguration.ClampRerollLimit(value).ToString();
	}

	private static void AddConfigTab(HBoxContainer tabs, List<Button> tabButtons, string text, Action action, bool compactLayout)
	{
		Button button = CreateTabButton(text, action, compactLayout);
		tabButtons.Add(button);
		tabs.AddChild(button);
	}

	private static Button CreateTabButton(string text, Action action, bool compactLayout)
	{
		Button button = CreateActionButton(text, action, compactLayout);
		button.CustomMinimumSize = compactLayout ? new Vector2(108f, 32f) : new Vector2(150f, 36f);
		return button;
	}

	private static void UpdateTabButtonStates(IReadOnlyList<Button> tabButtons, int selectedIndex, bool compactLayout)
	{
		for (int i = 0; i < tabButtons.Count; i++)
		{
			ApplyTabButtonState(tabButtons[i], i == selectedIndex, compactLayout);
		}
	}

	private static void ApplyTabButtonState(Button button, bool active, bool compactLayout)
	{
		Color normalBackground = active
			? new Color(0.18f, 0.21f, 0.28f, 0.98f)
			: new Color(0.1f, 0.12f, 0.17f, 0.9f);
		Color normalBorder = active
			? new Color(0.95f, 0.74f, 0.34f, 0.98f)
			: new Color(0.46f, 0.55f, 0.68f, 0.78f);
		button.AddThemeStyleboxOverride("normal", CreateButtonStyle(normalBackground, normalBorder));
		button.AddThemeStyleboxOverride("hover", CreateButtonStyle(new Color(0.16f, 0.19f, 0.26f, 0.98f), new Color(0.96f, 0.78f, 0.38f, 0.96f)));
		button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(new Color(0.09f, 0.11f, 0.16f, 0.98f), new Color(0.88f, 0.62f, 0.28f, 0.92f)));
		button.AddThemeStyleboxOverride("focus", CreateButtonStyle(new Color(0.16f, 0.19f, 0.26f, 0.98f), new Color(0.96f, 0.78f, 0.38f, 0.96f)));
		if (button.GetChildCount() > 0 && button.GetChild(0) is Label label)
		{
			label.Modulate = active
				? new Color(1f, 0.94f, 0.78f, 1f)
				: new Color(0.96f, 0.94f, 0.88f, 1f);
		}
	}

	private static Control CreateConfigPageActions(
		IReadOnlyList<RuneConfigEntry> playerEntries,
		IReadOnlyList<RuneConfigEntry> enemyEntries,
		IReadOnlyList<RuneConfigEntry> forgeEntries,
		HashSet<string> pendingDisabledPlayerIds,
		HashSet<string> pendingDisabledMonsterHexIds,
		HashSet<string> pendingDisabledForgeIds,
		int[] pendingPlayerHexCounts,
		int[] pendingEnemyHexCounts,
		int[] pendingPlayerRuneRerollLimit,
		int[] pendingMonsterHexRerollLimit,
		int[] pendingFirstActRuneWeights,
		int[] pendingNormalRuneWeights,
		int[] pendingSecondActAfterSilverWeights,
		int[] pendingForgeWeights,
		int[] pendingForgePrice,
		bool[] pendingShowHiddenRelicsToggle,
		IReadOnlyList<NumericValueBinding> numericBindings,
		IReadOnlyList<BooleanValueBinding> booleanBindings,
		IReadOnlyList<RuneIconBinding> playerIconBindings,
		IReadOnlyList<RuneIconBinding> enemyIconBindings,
		IReadOnlyList<RuneIconBinding> forgeIconBindings,
		Action updateSummary,
		Func<int> getPageIndex,
		bool compactLayout,
		out Action<int> updatePageActions)
	{
		HBoxContainer toolbar = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		toolbar.AddThemeConstantOverride("separation", compactLayout ? 7 : 12);

		Button enableAll = CreateActionButton(L("HEXTECH_CONFIG_ENABLE_ALL"), () =>
		{
			switch (getPageIndex())
			{
				case 1:
					pendingDisabledPlayerIds.Clear();
					pendingDisabledMonsterHexIds.Clear();
					UpdateAllRuneIcons(playerIconBindings, pendingDisabledPlayerIds);
					UpdateAllRuneIcons(enemyIconBindings, pendingDisabledMonsterHexIds);
					break;
				case 2:
					pendingDisabledForgeIds.Clear();
					UpdateAllRuneIcons(forgeIconBindings, pendingDisabledForgeIds);
					break;
			}

			updateSummary();
		}, compactLayout);
		Button disableAll = CreateActionButton(L("HEXTECH_CONFIG_DISABLE_ALL"), () =>
		{
			switch (getPageIndex())
			{
				case 1:
					ReplaceDisabledIds(pendingDisabledPlayerIds, playerEntries);
					ReplaceDisabledIds(pendingDisabledMonsterHexIds, enemyEntries);
					UpdateAllRuneIcons(playerIconBindings, pendingDisabledPlayerIds);
					UpdateAllRuneIcons(enemyIconBindings, pendingDisabledMonsterHexIds);
					break;
				case 2:
					ReplaceDisabledIds(pendingDisabledForgeIds, forgeEntries);
					UpdateAllRuneIcons(forgeIconBindings, pendingDisabledForgeIds);
					break;
			}

			updateSummary();
		}, compactLayout);
		toolbar.AddChild(enableAll);
		toolbar.AddChild(disableAll);
		toolbar.AddChild(CreateActionButton(L("HEXTECH_CONFIG_RESET"), () =>
		{
			HextechRunConfigurationSnapshot defaults = HextechRuneConfiguration.GetDefaultSnapshot();
			CopyArray(defaults.PlayerHexCountsByAct, pendingPlayerHexCounts);
			CopyArray(defaults.EnemyHexCountsByAct, pendingEnemyHexCounts);
			pendingPlayerRuneRerollLimit[0] = defaults.PlayerRuneRerollLimit;
			pendingMonsterHexRerollLimit[0] = defaults.MonsterHexRerollLimit;
			CopyArray(ToWeightArray(defaults.FirstActRuneRarityWeights), pendingFirstActRuneWeights);
			CopyArray(ToWeightArray(defaults.NormalRuneRarityWeights), pendingNormalRuneWeights);
			CopyArray(ToWeightArray(defaults.SecondActAfterSilverRuneRarityWeights), pendingSecondActAfterSilverWeights);
			CopyArray(ToWeightArray(defaults.ForgeRarityWeights), pendingForgeWeights);
			pendingForgePrice[0] = defaults.RandomForgeShopPrice;
			pendingShowHiddenRelicsToggle[0] = HextechRelicVisibilityHooks.GetDefaultShowHiddenRelicsToggle();
			pendingDisabledPlayerIds.Clear();
			pendingDisabledPlayerIds.UnionWith(defaults.DisabledPlayerRuneIds);
			pendingDisabledMonsterHexIds.Clear();
			pendingDisabledMonsterHexIds.UnionWith(defaults.DisabledMonsterHexIds);
			pendingDisabledForgeIds.Clear();
			pendingDisabledForgeIds.UnionWith(defaults.DisabledForgeIds);
			UpdateNumericLabels(numericBindings);
			UpdateBooleanToggles(booleanBindings);
			UpdateAllRuneIcons(playerIconBindings, pendingDisabledPlayerIds);
			UpdateAllRuneIcons(enemyIconBindings, pendingDisabledMonsterHexIds);
			UpdateAllRuneIcons(forgeIconBindings, pendingDisabledForgeIds);
			updateSummary();
		}, compactLayout));

		updatePageActions = pageIndex =>
		{
			bool showPoolBulkActions = pageIndex is 1 or 2;
			enableAll.Visible = showPoolBulkActions;
			disableAll.Visible = showPoolBulkActions;
		};
		return toolbar;
	}

	private static Control CreateConfigFooter(
		Control overlay,
		int[] pendingPlayerHexCounts,
		int[] pendingEnemyHexCounts,
		HashSet<string> pendingDisabledPlayerIds,
		HashSet<string> pendingDisabledMonsterHexIds,
		HashSet<string> pendingDisabledForgeIds,
		int[] pendingFirstActRuneWeights,
		int[] pendingNormalRuneWeights,
		int[] pendingSecondActAfterSilverWeights,
		int[] pendingForgeWeights,
		int[] pendingForgePrice,
		int[] pendingPlayerRuneRerollLimit,
		int[] pendingMonsterHexRerollLimit,
		bool[] pendingShowHiddenRelicsToggle,
		bool compactLayout)
	{
		HBoxContainer footer = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		footer.AddThemeConstantOverride("separation", compactLayout ? 7 : 12);
		footer.AddChild(CreateActionButton(L("HEXTECH_CONFIG_SAVE_CLOSE"), () =>
		{
			HextechRuneConfiguration.SaveSnapshot(new HextechRunConfigurationSnapshot(
				pendingPlayerHexCounts,
				pendingEnemyHexCounts,
				pendingPlayerRuneRerollLimit[0],
				pendingMonsterHexRerollLimit[0],
				pendingDisabledPlayerIds,
				pendingDisabledMonsterHexIds,
				pendingDisabledForgeIds,
				ToRarityWeights(pendingFirstActRuneWeights),
				ToRarityWeights(pendingNormalRuneWeights),
				ToRarityWeights(pendingSecondActAfterSilverWeights),
				ToForgeRarityWeights(pendingForgeWeights),
				pendingForgePrice[0]));
			HextechRelicVisibilityHooks.SetShowHiddenRelicsToggle(pendingShowHiddenRelicsToggle[0]);
			CollectionHooks.RefreshOpenRelicCollections();
			Log.Info($"[{ModInfo.Id}][RuneConfig] Saved run config: playerDisabled={pendingDisabledPlayerIds.Count} enemyDisabled={pendingDisabledMonsterHexIds.Count} forgeDisabled={pendingDisabledForgeIds.Count} playerCounts={string.Join(",", pendingPlayerHexCounts)} enemyCounts={string.Join(",", pendingEnemyHexCounts)} playerRerolls={pendingPlayerRuneRerollLimit[0]} monsterRerolls={pendingMonsterHexRerollLimit[0]} forgePrice={pendingForgePrice[0]} showHiddenUiToggle={pendingShowHiddenRelicsToggle[0]}");
			overlay.QueueFree();
		}, compactLayout));
		footer.AddChild(CreateActionButton(L("HEXTECH_CONFIG_CANCEL"), () => CloseWithoutSaving(overlay), compactLayout));
		return footer;
	}

	private static void ReplaceDisabledIds(HashSet<string> target, IEnumerable<RuneConfigEntry> entries)
	{
		target.Clear();
		foreach (RuneConfigEntry entry in entries)
		{
			target.Add(entry.Id);
		}
	}

	private static void CopyArray(IReadOnlyList<int> source, int[] target)
	{
		for (int i = 0; i < Math.Min(source.Count, target.Length); i++)
		{
			target[i] = source[i];
		}
	}

	private static Vector2 GetResponsivePanelSize()
	{
		Vector2I windowSize = DisplayServer.WindowGetSize();
		float windowWidth = windowSize.X > 0 ? windowSize.X : 1280f;
		float windowHeight = windowSize.Y > 0 ? windowSize.Y : 720f;
		float width = windowWidth < 760f
			? Math.Max(320f, windowWidth * 0.96f)
			: Mathf.Clamp(windowWidth * 0.9f, 760f, 1080f);
		float height = windowHeight < CompactConfigHeightThreshold
			? Math.Max(440f, windowHeight * 0.98f)
			: Mathf.Clamp(windowHeight * 0.92f, 660f, 840f);
		return new Vector2(width, height);
	}

	private static bool IsCompactConfigLayout()
	{
		Vector2I windowSize = DisplayServer.WindowGetSize();
		float windowHeight = windowSize.Y > 0 ? windowSize.Y : 720f;
		return windowHeight < CompactConfigHeightThreshold;
	}

	private static Control CreateEnemyHexCountSection(int[] pendingCounts, List<EnemyHexCountBinding> countBindings, bool readOnly, bool compactLayout)
	{
		VBoxContainer section = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		section.AddThemeConstantOverride("separation", compactLayout ? 5 : 8);

		Label title = CreateSectionHeader(L("HEXTECH_ENEMY_COUNT_TITLE"), compactLayout ? 18 : 20);
		section.AddChild(title);

		if (!compactLayout)
		{
			Label description = CreateLabel(
				L(readOnly ? "HEXTECH_ENEMY_COUNT_CLIENT_READONLY" : "HEXTECH_ENEMY_COUNT_DESCRIPTION"),
				14,
				new Color(0.78f, 0.84f, 0.9f, 0.9f));
			description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			section.AddChild(description);
		}

		HBoxContainer row = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		row.AddThemeConstantOverride("separation", compactLayout ? 10 : 18);
		section.AddChild(row);

		row.AddChild(CreateEnemyHexCountStepper(L("HEXTECH_ENEMY_COUNT_ACT1"), 0, pendingCounts, countBindings, readOnly, compactLayout));
		row.AddChild(CreateEnemyHexCountStepper(L("HEXTECH_ENEMY_COUNT_ACT2"), 1, pendingCounts, countBindings, readOnly, compactLayout));
		row.AddChild(CreateEnemyHexCountStepper(L("HEXTECH_ENEMY_COUNT_ACT3"), 2, pendingCounts, countBindings, readOnly, compactLayout));
		return section;
	}

	private static Control CreateEnemyHexCountStepper(string labelText, int actIndex, int[] pendingCounts, List<EnemyHexCountBinding> countBindings, bool readOnly, bool compactLayout)
	{
		VBoxContainer root = new()
		{
			CustomMinimumSize = compactLayout ? new Vector2(150f, 58f) : new Vector2(190f, 70f),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		root.AddThemeConstantOverride("separation", compactLayout ? 3 : 5);

		Label label = CreateLabel(labelText, compactLayout ? 13 : 15, new Color(0.92f, 0.9f, 0.78f, 0.96f));
		label.HorizontalAlignment = HorizontalAlignment.Center;
		root.AddChild(label);

		HBoxContainer controls = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		controls.AddThemeConstantOverride("separation", compactLayout ? 6 : 8);
		root.AddChild(controls);

		Label number = CreateLabel(pendingCounts[actIndex].ToString(), compactLayout ? 17 : 18, new Color(0.98f, 0.98f, 0.94f, 1f));
		number.HorizontalAlignment = HorizontalAlignment.Center;
		number.VerticalAlignment = VerticalAlignment.Center;
		number.CustomMinimumSize = compactLayout ? new Vector2(36f, 32f) : new Vector2(42f, 34f);
		countBindings.Add(new EnemyHexCountBinding(actIndex, number));

		Button minus = CreateStepButton("-", readOnly, compactLayout);
		Button plus = CreateStepButton("+", readOnly, compactLayout);
		AttachRepeatingStep(minus, () =>
		{
			pendingCounts[actIndex] = HextechRuneConfiguration.ClampEnemyHexCount(pendingCounts[actIndex] - 1);
			SetLabelText(number, pendingCounts[actIndex].ToString());
		});
		AttachRepeatingStep(plus, () =>
		{
			pendingCounts[actIndex] = HextechRuneConfiguration.ClampEnemyHexCount(pendingCounts[actIndex] + 1);
			SetLabelText(number, pendingCounts[actIndex].ToString());
		});

		controls.AddChild(minus);
		controls.AddChild(number);
		controls.AddChild(plus);
		return root;
	}

	private static Button CreateStepButton(string text, bool disabled, bool compactLayout)
	{
		Button button = new()
		{
			Text = string.Empty,
			CustomMinimumSize = compactLayout ? new Vector2(34f, 32f) : new Vector2(38f, 34f),
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			Disabled = disabled
		};
		button.AddThemeStyleboxOverride("normal", CreateButtonStyle(new Color(0.1f, 0.12f, 0.17f, 0.9f), new Color(0.46f, 0.55f, 0.68f, 0.78f)));
		button.AddThemeStyleboxOverride("hover", CreateButtonStyle(new Color(0.13f, 0.16f, 0.22f, 0.95f), new Color(0.88f, 0.72f, 0.36f, 0.92f)));
		button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(new Color(0.07f, 0.09f, 0.13f, 0.98f), new Color(0.88f, 0.62f, 0.28f, 0.92f)));
		button.AddThemeStyleboxOverride("disabled", CreateButtonStyle(new Color(0.08f, 0.09f, 0.12f, 0.56f), new Color(0.32f, 0.36f, 0.44f, 0.58f)));
		AddCrispButtonText(button, text, compactLayout ? 17 : 18, disabled ? new Color(0.62f, 0.66f, 0.72f, 0.82f) : new Color(0.96f, 0.94f, 0.88f, 1f));
		return button;
	}

	private static void AttachRepeatingStep(Button button, Action action)
	{
		int pressToken = 0;

		button.ButtonDown += () =>
		{
			if (button.Disabled)
			{
				return;
			}

			pressToken++;
			int currentToken = pressToken;
			action();
			_ = RepeatStepAsync(button, currentToken, () => pressToken == currentToken, action);
		};
		button.ButtonUp += () => pressToken++;
		button.TreeExiting += () => pressToken++;
	}

	private static async Task RepeatStepAsync(Button button, int token, Func<bool> tokenIsCurrent, Action action)
	{
		if (!GodotObject.IsInstanceValid(button) || !button.IsInsideTree())
		{
			return;
		}

		SceneTree tree = button.GetTree();
		if (tree == null)
		{
			return;
		}

		await button.ToSignal(tree.CreateTimer(StepRepeatInitialDelaySeconds), "timeout");
		int repeatCount = 0;
		while (GodotObject.IsInstanceValid(button)
			&& button.IsInsideTree()
			&& button.ButtonPressed
			&& !button.Disabled
			&& tokenIsCurrent())
		{
			action();
			repeatCount++;
			float interval = repeatCount >= StepRepeatFastAfterTicks
				? StepRepeatFastIntervalSeconds
				: StepRepeatIntervalSeconds;
			await button.ToSignal(tree.CreateTimer(interval), "timeout");
		}
	}

	private static bool IsEnemyHexCountConfigReadOnly()
	{
		try
		{
			return RunManager.Instance?.NetService.Type == NetGameType.Client;
		}
		catch
		{
			return false;
		}
	}

	private static Control CreateCompactHelpSection(bool enemyHexCountReadOnly)
	{
		VBoxContainer section = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		section.AddThemeConstantOverride("separation", 5);

		string enemyHelp = L(enemyHexCountReadOnly ? "HEXTECH_ENEMY_COUNT_CLIENT_READONLY" : "HEXTECH_ENEMY_COUNT_DESCRIPTION");
		Label help = CreateLabel(enemyHelp + "\n" + L("HEXTECH_CONFIG_DESCRIPTION"), 13, new Color(0.78f, 0.84f, 0.9f, 0.9f));
		help.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		help.Visible = false;

		Button? toggle = null;
		toggle = CreateActionButton(L("HEXTECH_CONFIG_SHOW_HELP"), () =>
		{
			help.Visible = !help.Visible;
			if (toggle != null)
			{
				SetButtonDisplayText(toggle, L(help.Visible ? "HEXTECH_CONFIG_HIDE_HELP" : "HEXTECH_CONFIG_SHOW_HELP"));
			}
		}, compactLayout: true);
		toggle.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		section.AddChild(toggle);
		section.AddChild(help);
		return section;
	}

	private static Control CreateToolbar(
		Control overlay,
		IReadOnlyList<RuneConfigEntry> entries,
		HashSet<string> pendingDisabledIds,
		int[] pendingEnemyHexCounts,
		IReadOnlyList<EnemyHexCountBinding> enemyHexCountBindings,
		IReadOnlyList<RuneIconBinding> iconBindings,
		Label summary,
		bool compactLayout)
	{
		HBoxContainer toolbar = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		toolbar.AddThemeConstantOverride("separation", compactLayout ? 7 : 12);

		toolbar.AddChild(CreateActionButton(L("HEXTECH_CONFIG_ENABLE_ALL"), () =>
		{
			pendingDisabledIds.Clear();
			UpdateAllRuneIcons(iconBindings, pendingDisabledIds);
			UpdateSummary(summary, pendingDisabledIds);
		}, compactLayout));
		toolbar.AddChild(CreateActionButton(L("HEXTECH_CONFIG_DISABLE_ALL"), () =>
		{
			foreach (RuneConfigEntry entry in entries)
			{
				pendingDisabledIds.Add(entry.Id);
			}

			UpdateAllRuneIcons(iconBindings, pendingDisabledIds);
			UpdateSummary(summary, pendingDisabledIds);
		}, compactLayout));
		toolbar.AddChild(CreateActionButton(L("HEXTECH_CONFIG_RESET"), () =>
		{
			pendingDisabledIds.Clear();
			pendingDisabledIds.UnionWith(HextechRuneConfiguration.GetDefaultDisabledPlayerRuneIds());
			int[] defaultEnemyHexCounts = HextechRuneConfiguration.GetDefaultEnemyHexCountsByAct();
			for (int i = 0; i < Math.Min(pendingEnemyHexCounts.Length, defaultEnemyHexCounts.Length); i++)
			{
				pendingEnemyHexCounts[i] = defaultEnemyHexCounts[i];
			}

			UpdateEnemyHexCountLabels(enemyHexCountBindings, pendingEnemyHexCounts);
			UpdateAllRuneIcons(iconBindings, pendingDisabledIds);
			UpdateSummary(summary, pendingDisabledIds);
		}, compactLayout));
		toolbar.AddChild(CreateActionButton(L("HEXTECH_CONFIG_SAVE_CLOSE"), () =>
		{
			HextechRuneConfiguration.SaveDisabledPlayerRuneIds(pendingDisabledIds);
			HextechRuneConfiguration.SaveEnemyHexCountsByAct(pendingEnemyHexCounts);
			CollectionHooks.RefreshOpenRelicCollections();
			Log.Info($"[{ModInfo.Id}][RuneConfig] Saved player rune config: disabled={pendingDisabledIds.Count} enemyCounts={string.Join(",", pendingEnemyHexCounts)}");
			overlay.QueueFree();
		}, compactLayout));
		toolbar.AddChild(CreateActionButton(L("HEXTECH_CONFIG_CANCEL"), () => CloseWithoutSaving(overlay), compactLayout));
		return toolbar;
	}

	private static void CloseWithoutSaving(Control overlay)
	{
		if (!GodotObject.IsInstanceValid(overlay))
		{
			return;
		}

		overlay.GetViewport()?.SetInputAsHandled();
		overlay.QueueFree();
	}

	private static Label CreateSectionHeader(string text, int fontSize = 20)
	{
		Label label = CreateLabel(text, fontSize, new Color(0.96f, 0.84f, 0.48f, 0.98f));
		label.CustomMinimumSize = new Vector2(0f, fontSize + 6f);
		return label;
	}

	private static Label CreateSourceHeader(string text, bool compactLayout)
	{
		Label label = CreateLabel(text, compactLayout ? 14 : 15, new Color(0.68f, 0.82f, 0.98f, 0.92f));
		label.CustomMinimumSize = new Vector2(0f, compactLayout ? 18f : 22f);
		return label;
	}

	private static VBoxContainer CreateRuneGrid(bool compactLayout)
	{
		VBoxContainer grid = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		grid.AddThemeConstantOverride("separation", compactLayout ? 5 : 7);
		return grid;
	}

	private static HBoxContainer CreateRuneRow(bool compactLayout)
	{
		HBoxContainer row = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		row.AddThemeConstantOverride("separation", compactLayout ? 6 : 8);
		return row;
	}

	private static CenterContainer CreateRuneSlot()
	{
		return new CenterContainer()
		{
			CustomMinimumSize = new Vector2(RuneConfigCellWidth, RuneConfigCellHeight),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
	}

	private static RuneIconBinding CreateRuneIcon(RuneConfigEntry entry, HashSet<string> pendingDisabledIds, Action updateSummary)
	{
		VBoxContainer root = new()
		{
			Name = "RuneConfigIcon_" + entry.Id,
			CustomMinimumSize = new Vector2(RuneConfigCellWidth, RuneConfigCellHeight),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			MouseFilter = Control.MouseFilterEnum.Stop,
			FocusMode = Control.FocusModeEnum.All,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			Alignment = BoxContainer.AlignmentMode.Center
		};
		root.AddThemeConstantOverride("separation", 2);

		Control iconLayer = new()
		{
			CustomMinimumSize = new Vector2(RuneConfigCellWidth, RuneConfigIconLayerHeight),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		CenterContainer iconCenter = new()
		{
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		iconCenter.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		ApplyConfigIconScale(iconCenter);
		NRelicBasicHolder holder = NRelicBasicHolder.Create(entry.Relic)
			?? throw new InvalidOperationException($"Failed to create config relic holder for {entry.Id}.");
		holder.MouseFilter = Control.MouseFilterEnum.Ignore;
		iconCenter.AddChild(holder);
		iconLayer.AddChild(iconCenter);
		root.AddChild(iconLayer);

		Label title = CreateRuneNameLabel(entry.Title);
		root.AddChild(title);

		RuneIconBinding binding = new(entry.Id, root, holder, title);
		ApplyRuneIconState(binding, !pendingDisabledIds.Contains(entry.Id));
		AttachRuneToggleInput(root, entry, binding, pendingDisabledIds, updateSummary);
		AttachRelicHoverTips(root, entry.Relic, GetEnemyHexKind(entry));
		return binding;
	}

	private static void ApplyConfigIconScale(Control control)
	{
		control.Scale = Vector2.One * ConfigRuneHolderScale;
		control.PivotOffset = new Vector2(RuneConfigCellWidth, RuneConfigIconLayerHeight) * 0.5f;
		control.Resized += () =>
		{
			if (GodotObject.IsInstanceValid(control))
			{
				control.PivotOffset = control.Size * 0.5f;
			}
		};
	}

	private static async Task PopulateRuneIconsAsync(Control overlay, RuneConfigOverlayState state)
	{
		if (!await AwaitProcessFrameAsync(overlay))
		{
			return;
		}

		int loadedThisFrame = 0;
		foreach (RuneConfigLoadTarget target in state.LoadTargets)
		{
			if (!GodotObject.IsInstanceValid(overlay) || !overlay.IsInsideTree())
			{
				return;
			}

			RuneIconBinding binding = CreateRuneIcon(target.Entry, target.PendingDisabledIds, state.UpdateSummary);
			if (ReferenceEquals(target.PendingDisabledIds, state.PendingDisabledPlayerIds))
			{
				state.PlayerIconBindings.Add(binding);
			}
			else if (ReferenceEquals(target.PendingDisabledIds, state.PendingDisabledMonsterHexIds))
			{
				state.EnemyIconBindings.Add(binding);
			}
			else
			{
				state.ForgeIconBindings.Add(binding);
			}
			target.Grid.AddChild(binding.Root);

			loadedThisFrame++;
			if (loadedThisFrame < RuneConfigIconsPerFrame)
			{
				continue;
			}

			loadedThisFrame = 0;
			if (!await AwaitProcessFrameAsync(overlay))
			{
				return;
			}
		}
	}

	private static Label CreateRuneNameLabel(string text)
	{
		Label label = CreateLabel(text, 13, new Color(0.96f, 0.97f, 1f, 1f));
		label.CustomMinimumSize = new Vector2(108f, 38f);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Top;
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.ClipText = true;
		label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.82f));
		label.AddThemeConstantOverride("outline_size", 2);
		return label;
	}

	private static void AttachRuneToggleInput(
		Control root,
		RuneConfigEntry entry,
		RuneIconBinding binding,
		HashSet<string> pendingDisabledIds,
		Action updateSummary)
	{
		bool pointerPressed = false;
		bool pointerDragged = false;
		bool longPressShown = false;
		int pointerToken = 0;
		Vector2 pressPosition = Vector2.Zero;

		root.GuiInput += inputEvent =>
		{
			switch (inputEvent)
			{
				case InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton:
					if (mouseButton.Pressed)
					{
						BeginRunePress(mouseButton.Position, false);
					}
					else
					{
						EndRunePress();
					}
					break;
				case InputEventMouseMotion mouseMotion when pointerPressed:
					UpdateRuneDrag(mouseMotion.Position);
					break;
				case InputEventScreenTouch screenTouch:
					if (screenTouch.Pressed)
					{
						BeginRunePress(screenTouch.Position, true);
					}
					else
					{
						EndRunePress();
					}
					break;
				case InputEventScreenDrag screenDrag when pointerPressed:
					UpdateRuneDrag(screenDrag.Position);
					break;
			}
		};

		void BeginRunePress(Vector2 position, bool touch)
		{
			pointerPressed = true;
			pointerDragged = false;
			longPressShown = false;
			pressPosition = position;
			pointerToken++;
			if (touch)
			{
				int currentToken = pointerToken;
				_ = ShowTouchHoverTipAfterDelay(root, entry.Relic, GetEnemyHexKind(entry), currentToken, () => pointerToken == currentToken && pointerPressed && !pointerDragged, () => longPressShown = true);
			}
		}

		void UpdateRuneDrag(Vector2 position)
		{
			if (pressPosition.DistanceTo(position) <= RuneConfigDragThreshold)
			{
				return;
			}

			pointerDragged = true;
			NHoverTipSet.Remove(root);
		}

		void EndRunePress()
		{
			if (!pointerPressed)
			{
				return;
			}

			pointerPressed = false;
			pointerToken++;
			if (!pointerDragged && !longPressShown)
			{
				root.GetViewport()?.SetInputAsHandled();
				ToggleRune(entry.Id, binding, pendingDisabledIds, updateSummary);
			}
			else if (longPressShown)
			{
				root.GetViewport()?.SetInputAsHandled();
			}

			NHoverTipSet.Remove(root);
		}
	}

	private static async Task ShowTouchHoverTipAfterDelay(
		Control holder,
		RelicModel relic,
		MonsterHexKind? monsterHex,
		int token,
		Func<bool> shouldShow,
		Action onShown)
	{
		if (!GodotObject.IsInstanceValid(holder) || !holder.IsInsideTree())
		{
			return;
		}

		SceneTree tree = holder.GetTree();
		if (tree == null)
		{
			return;
		}

		await holder.ToSignal(tree.CreateTimer(RuneConfigLongPressSeconds), "timeout");
		if (!GodotObject.IsInstanceValid(holder) || !holder.IsInsideTree() || !shouldShow())
		{
			return;
		}

		ShowRelicHoverTips(holder, relic, monsterHex);
		onShown();
		holder.GetViewport()?.SetInputAsHandled();
	}

	private static Button CreateActionButton(string text, Action action, bool compactLayout = false)
	{
		Button button = new()
		{
			Text = string.Empty,
			CustomMinimumSize = compactLayout ? new Vector2(112f, 34f) : new Vector2(132f, 38f),
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		button.AddThemeStyleboxOverride("normal", CreateButtonStyle(new Color(0.1f, 0.12f, 0.17f, 0.9f), new Color(0.46f, 0.55f, 0.68f, 0.78f)));
		button.AddThemeStyleboxOverride("hover", CreateButtonStyle(new Color(0.13f, 0.16f, 0.22f, 0.95f), new Color(0.88f, 0.72f, 0.36f, 0.92f)));
		button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(new Color(0.07f, 0.09f, 0.13f, 0.98f), new Color(0.88f, 0.62f, 0.28f, 0.92f)));
		button.AddThemeStyleboxOverride("focus", CreateButtonStyle(new Color(0.13f, 0.16f, 0.22f, 0.95f), new Color(0.88f, 0.72f, 0.36f, 0.92f)));
		AddCrispButtonText(button, text, compactLayout ? 14 : 16, new Color(0.96f, 0.94f, 0.88f, 1f));
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

	private static void UpdateEnemyHexCountLabels(IReadOnlyList<EnemyHexCountBinding> bindings, IReadOnlyList<int> pendingEnemyHexCounts)
	{
		foreach (EnemyHexCountBinding binding in bindings)
		{
			if (binding.ActIndex >= 0 && binding.ActIndex < pendingEnemyHexCounts.Count)
			{
				SetLabelText(binding.Number, pendingEnemyHexCounts[binding.ActIndex].ToString());
			}
		}
	}

	private static void ApplyRuneIconState(RuneIconBinding binding, bool enabled)
	{
		binding.Holder.Modulate = enabled
			? Colors.White
			: new Color(0.34f, 0.36f, 0.4f, 0.44f);
		binding.Title.Modulate = enabled
			? Colors.White
			: new Color(0.6f, 0.64f, 0.72f, 0.58f);
	}

	private static void ToggleRune(string id, RuneIconBinding binding, HashSet<string> pendingDisabledIds, Action updateSummary)
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
		PlayRuneToggleFeedback(binding.Root);
		updateSummary();
	}

	private static void PlayRuneToggleFeedback(Control root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return;
		}

		root.PivotOffset = root.Size * 0.5f;
		Tween tween = root.CreateTween();
		tween.TweenProperty(root, "scale", Vector2.One * 1.06f, 0.055f);
		tween.TweenProperty(root, "scale", Vector2.One, 0.085f);
	}

	private static void AttachRelicHoverTips(Control holder, RelicModel relic, MonsterHexKind? monsterHex = null)
	{
		holder.MouseEntered += () => ShowRelicHoverTips(holder, relic, monsterHex);
		holder.MouseExited += () => NHoverTipSet.Remove(holder);
		holder.TreeExiting += () => NHoverTipSet.Remove(holder);
	}

	private static void ShowRelicHoverTips(Control holder, RelicModel relic, MonsterHexKind? monsterHex = null)
	{
		NHoverTipSet.Remove(holder);
		IEnumerable<IHoverTip> hoverTips = monsterHex.HasValue
			? MonsterHexCatalog.GetEnemyHexHoverTips(monsterHex.Value)
			: relic.HoverTips;
		NHoverTipSet? hoverTipSet = NHoverTipSet.CreateAndShow(holder, hoverTips, HoverTip.GetHoverTipAlignment(holder));
		if (hoverTipSet == null)
		{
			return;
		}

		hoverTipSet.ZIndex = HoverTipZIndex;
		hoverTipSet.ZAsRelative = false;
		hoverTipSet.SetAlignment(holder, HoverTip.GetHoverTipAlignment(holder));
	}

	private static MonsterHexKind? GetEnemyHexKind(RuneConfigEntry entry)
	{
		return entry.PoolKey == "ENEMY" && Enum.TryParse(entry.Id, out MonsterHexKind monsterHex)
			? monsterHex
			: null;
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
			string sourceKey = GetConfigSourceKey(id);
			string sourceText = GetConfigSourceText(id);
			entries.Add(new RuneConfigEntry(
				id.Entry,
				relic,
				relic.Title.GetFormattedText(),
				new LocString(LocTable, "HEXTECH_SERIES." + rarityKey).GetRawText(),
				new LocString(LocTable, "HEXTECH_POOL." + poolKey).GetRawText(),
				new LocString(LocTable, "HEXTECH_TAG." + tagKey).GetRawText(),
				(int)rarity,
				poolKey,
				tagKey,
				sourceKey,
				sourceText));
		}

		return entries
			.OrderBy(static entry => entry.RarityOrder)
			.ThenBy(static entry => entry.SourceKey, StringComparer.Ordinal)
			.ThenBy(static entry => entry.PoolKey, StringComparer.Ordinal)
			.ThenBy(static entry => entry.TagKey, StringComparer.Ordinal)
			.ThenBy(static entry => entry.Title, StringComparer.CurrentCulture)
			.ToList();
	}

	private static List<RuneConfigEntry> BuildEnemyHexEntries()
	{
		List<RuneConfigEntry> entries = [];
		foreach (MonsterHexKind kind in Enum.GetValues<HextechRarityTier>()
			.SelectMany(MonsterHexCatalog.GetMonsterHexesForRarity))
		{
			RelicModel relic = MonsterHexCatalog.GetIconRelicForMonsterHex(kind);
			HextechRarityTier rarity = MonsterHexCatalog.GetMonsterHexRarity(kind);
			string rarityKey = rarity.ToString().ToUpperInvariant();
			entries.Add(new RuneConfigEntry(
				kind.ToString(),
				relic,
				relic.Title.GetFormattedText(),
				new LocString(LocTable, "HEXTECH_SERIES." + rarityKey).GetRawText(),
				L("HEXTECH_ENEMY_POOL_TITLE"),
				string.Empty,
				(int)rarity,
				"ENEMY",
				kind.ToString(),
				BaseConfigSourceKey,
				L("HEXTECH_CONFIG_SOURCE_BASE")));
		}

		return entries
			.OrderBy(static entry => entry.RarityOrder)
			.ThenBy(static entry => entry.Title, StringComparer.CurrentCulture)
			.ToList();
	}

	private static List<RuneConfigEntry> BuildForgeEntries()
	{
		List<RuneConfigEntry> entries = [];
		foreach (Type forgeType in HextechCatalog.GetAllForgeTypes())
		{
			RelicModel relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(forgeType));
			ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
			HextechRarityTier rarity = HextechCatalog.TryGetForgeRarity(relic, out HextechRarityTier resolvedRarity)
				? resolvedRarity
				: HextechRarityTier.Gold;
			string rarityKey = rarity.ToString().ToUpperInvariant();
			string sourceKey = GetConfigSourceKey(id);
			string sourceText = GetConfigSourceText(id);
			entries.Add(new RuneConfigEntry(
				id.Entry,
				relic,
				relic.Title.GetFormattedText(),
				new LocString(LocTable, "HEXTECH_SERIES." + rarityKey).GetRawText(),
				L("HEXTECH_CONFIG_TAB_FORGES"),
				string.Empty,
				(int)rarity,
				"FORGE",
				forgeType.Name,
				sourceKey,
				sourceText));
		}

		return entries
			.OrderBy(static entry => entry.RarityOrder)
			.ThenBy(static entry => entry.SourceKey, StringComparer.Ordinal)
			.ThenBy(static entry => entry.Title, StringComparer.CurrentCulture)
			.ToList();
	}

	private static string GetConfigSourceKey(ModelId id)
	{
		string? assetModId = HextechExternalContentRegistry.GetAssetModId(id);
		return string.IsNullOrWhiteSpace(assetModId) || string.Equals(assetModId, ModInfo.Id, StringComparison.Ordinal)
			? BaseConfigSourceKey
			: ExternalConfigSourcePrefix + assetModId;
	}

	private static string GetConfigSourceText(ModelId id)
	{
		string? assetModId = HextechExternalContentRegistry.GetAssetModId(id);
		if (string.IsNullOrWhiteSpace(assetModId) || string.Equals(assetModId, ModInfo.Id, StringComparison.Ordinal))
		{
			return L("HEXTECH_CONFIG_SOURCE_BASE");
		}

		if (string.Equals(assetModId, SponsorPackModId, StringComparison.Ordinal))
		{
			return L("HEXTECH_CONFIG_SOURCE_EXTRA_PACK");
		}

		return string.Format(L("HEXTECH_CONFIG_SOURCE_EXTERNAL"), assetModId);
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

	private static void UpdateNumericLabels(IReadOnlyList<NumericValueBinding> bindings)
	{
		foreach (NumericValueBinding binding in bindings)
		{
			SetLabelText(binding.Number, binding.GetText());
		}
	}

	private static void UpdateBooleanToggles(IReadOnlyList<BooleanValueBinding> bindings)
	{
		foreach (BooleanValueBinding binding in bindings)
		{
			binding.Toggle.SetPressedNoSignal(binding.GetValue());
		}
	}

	private static void UpdateSummary(
		Label summary,
		int pageIndex,
		IReadOnlySet<string> pendingDisabledPlayerIds,
		IReadOnlySet<string> pendingDisabledMonsterHexIds,
		IReadOnlySet<string> pendingDisabledForgeIds)
	{
		HashSet<string> configurableIds = HextechCatalog.GetConfigurablePlayerRuneIds()
			.Select(static id => id.Entry)
			.ToHashSet(StringComparer.Ordinal);
		int playerTotal = configurableIds.Count;
		int playerDisabled = pendingDisabledPlayerIds.Count(configurableIds.Contains);
		int playerEnabled = Math.Max(0, playerTotal - playerDisabled);
		int enemyTotal = Enum.GetValues<HextechRarityTier>()
			.SelectMany(MonsterHexCatalog.GetMonsterHexesForRarity)
			.Count();
		int enemyDisabled = pendingDisabledMonsterHexIds.Count;
		int enemyEnabled = Math.Max(0, enemyTotal - enemyDisabled);
		HashSet<string> forgeIds = HextechCatalog.GetAllForgeTypes()
			.Select(ModelDb.GetId)
			.Select(static id => id.Entry)
			.ToHashSet(StringComparer.Ordinal);
		int forgeTotal = forgeIds.Count;
		int forgeDisabled = pendingDisabledForgeIds.Count(forgeIds.Contains);
		int forgeEnabled = Math.Max(0, forgeTotal - forgeDisabled);
		string text = pageIndex switch
		{
			1 => $"{L("HEXTECH_PLAYER_POOL_TITLE")} {playerEnabled}/{playerTotal}  |  {L("HEXTECH_ENEMY_POOL_TITLE")} {enemyEnabled}/{enemyTotal}",
			2 => $"{L("HEXTECH_CONFIG_TAB_FORGES")} {forgeEnabled}/{forgeTotal}",
			_ => string.Empty
		};
		summary.Visible = text.Length > 0;
		SetLabelText(summary, text);
	}

	private static void UpdateSummary(Label summary, IReadOnlySet<string> pendingDisabledIds)
	{
		UpdateSummary(summary, 1, pendingDisabledIds, new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal));
	}

	private static Label CreateLabel(string text, int fontSize, Color color)
	{
		MegaLabel label = new()
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			MinFontSize = fontSize,
			MaxFontSize = fontSize
		};
		ApplyDefaultMegaLabelTheme(label);
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.Modulate = color;
		label.AddThemeColorOverride("font_color", Colors.White);
		label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.68f));
		label.AddThemeConstantOverride("outline_size", 2);
		label.SetTextAutoSize(text);
		return label;
	}

	private static void SetLabelText(Label label, string text)
	{
		if (label is MegaLabel megaLabel)
		{
			megaLabel.SetTextAutoSize(text);
			return;
		}

		label.Text = text;
	}

	private static void SetButtonDisplayText(Button button, string text)
	{
		if (button.GetChildCount() > 0 && button.GetChild(0) is Label label)
		{
			SetLabelText(label, text);
			return;
		}

		button.Text = text;
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

	private static void AddCrispButtonText(Button button, string text, int fontSize, Color fontColor)
	{
		MegaLabel label = new()
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MinFontSize = fontSize,
			MaxFontSize = fontSize
		};
		label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		ApplyDefaultMegaLabelTheme(label);
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", fontColor);
		label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.62f));
		label.AddThemeConstantOverride("outline_size", 2);
		label.SetTextAutoSize(text);
		button.AddChild(label);
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
		string TagKey,
		string SourceKey,
		string SourceText);

	private sealed record RuneConfigLoadTarget(
		RuneConfigEntry Entry,
		Container Grid,
		HashSet<string> PendingDisabledIds);

	private sealed record EnemyHexCountBinding(
		int ActIndex,
		Label Number);

	private sealed record RuneConfigOverlayState(
		IReadOnlyList<RuneConfigLoadTarget> LoadTargets,
		HashSet<string> PendingDisabledPlayerIds,
		HashSet<string> PendingDisabledMonsterHexIds,
		HashSet<string> PendingDisabledForgeIds,
		List<RuneIconBinding> PlayerIconBindings,
		List<RuneIconBinding> EnemyIconBindings,
		List<RuneIconBinding> ForgeIconBindings,
		Action UpdateSummary);

	private sealed record RuneIconBinding(
		string Id,
		Control Root,
		Control Holder,
		Label Title);

	private sealed record NumericValueBinding(
		Func<string> GetText,
		Label Number);

	private sealed record BooleanValueBinding(
		Func<bool> GetValue,
		BaseButton Toggle);

}
