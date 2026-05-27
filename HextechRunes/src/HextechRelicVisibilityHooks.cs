using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Runs;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechRelicVisibilityHooks
{
	private const string ConfigFileName = "ui_config.json";
	private const string ToggleRootNodeName = "HextechHideRelicsToggleRoot";
	private const string ToggleColumnNodeName = "HextechHideRelicsToggleColumn";
	private const string ToggleBoxNodeName = "HextechHideRelicsToggleBox";
	private const string ToggleVisualsNodeName = "TickboxVisuals";
	private const string ToggleButtonNodeName = "HextechHideRelicsToggleButton";
	private const string ToggleLabelNodeName = "HextechHideRelicsToggleLabel";
	private const string PositionTimerNodeName = "HextechHideRelicsTogglePositionTimer";
	private const string TickboxVisualScenePath = "res://scenes/ui/tickbox.tscn";
	private const BindingFlags CombatUiFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
	private static readonly Vector2 ToggleRootSize = new(72f, 80f);
	private static readonly Vector2 ToggleBoxSize = new(64f, 64f);
	private const float DrawPileGap = 10f;
	private const float BottomFallbackPadding = 34f;
	private const float LeftFallbackPadding = 150f;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true
	};

	private static bool _installed;
	private static NDrawPileButton? _drawPileAnchor;
	private static ModUiConfig _config = new();

	public static void Install(Harmony harmony)
	{
		if (_installed)
		{
			return;
		}

		_config = LoadOrCreateConfig();
		harmony.Patch(
			RequireMethod(typeof(NGlobalUi), nameof(NGlobalUi.Initialize), BindingFlags.Instance | BindingFlags.Public, typeof(RunState)),
			postfix: new HarmonyMethod(typeof(HextechRelicVisibilityHooks), nameof(NGlobalUiInitializePostfix)));
		harmony.Patch(
			RequireMethod(typeof(NCombatUi), nameof(NCombatUi._Ready), CombatUiFlags),
			postfix: new HarmonyMethod(typeof(HextechRelicVisibilityHooks), nameof(NCombatUiVisiblePostfix)));
		harmony.Patch(
			RequireMethod(typeof(NCombatUi), "AnimIn", CombatUiFlags),
			postfix: new HarmonyMethod(typeof(HextechRelicVisibilityHooks), nameof(NCombatUiVisiblePostfix)));
		harmony.Patch(
			RequireMethod(typeof(NCombatUi), nameof(NCombatUi.Enable), CombatUiFlags),
			postfix: new HarmonyMethod(typeof(HextechRelicVisibilityHooks), nameof(NCombatUiVisiblePostfix)));
		harmony.Patch(
			RequireMethod(typeof(NCombatUi), "AnimOut", CombatUiFlags),
			postfix: new HarmonyMethod(typeof(HextechRelicVisibilityHooks), nameof(NCombatUiHiddenPostfix)));
		harmony.Patch(
			RequireMethod(typeof(NCombatUi), nameof(NCombatUi.Disable), CombatUiFlags),
			postfix: new HarmonyMethod(typeof(HextechRelicVisibilityHooks), nameof(NCombatUiHiddenPostfix)));
		harmony.Patch(
			RequireMethod(typeof(NCombatUi), nameof(NCombatUi._ExitTree), CombatUiFlags),
			postfix: new HarmonyMethod(typeof(HextechRelicVisibilityHooks), nameof(NCombatUiHiddenPostfix)));
		harmony.Patch(
			RequireMethod(typeof(NRelicInventory), nameof(NRelicInventory.Initialize), BindingFlags.Instance | BindingFlags.Public, typeof(RunState)),
			postfix: new HarmonyMethod(typeof(HextechRelicVisibilityHooks), nameof(NRelicInventoryRefreshPostfix)));
		harmony.Patch(
			RequireMethod(typeof(NRelicInventory), "Add", BindingFlags.Instance | BindingFlags.NonPublic, typeof(RelicModel), typeof(bool), typeof(int)),
			postfix: new HarmonyMethod(typeof(HextechRelicVisibilityHooks), nameof(NRelicInventoryRefreshPostfix)));
		harmony.Patch(
			RequireMethod(typeof(NRelicInventory), nameof(NRelicInventory.AnimShow), BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechRelicVisibilityHooks), nameof(NRelicInventoryRefreshPostfix)));
		harmony.Patch(
			RequireMethod(typeof(NRelicInventory), nameof(NRelicInventory.ShowImmediately), BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechRelicVisibilityHooks), nameof(NRelicInventoryRefreshPostfix)));
		harmony.Patch(
			RequireMethod(typeof(NRelicInventoryHolder), "DoFlash", BindingFlags.Instance | BindingFlags.NonPublic),
			prefix: new HarmonyMethod(typeof(HextechRelicVisibilityHooks), nameof(NRelicInventoryHolderDoFlashPrefix)));

		_installed = true;
		Log.Info($"[{ModInfo.Id}][Mayhem] Relic visibility toggle loaded: hide_relics={_config.HideRelics}.");
	}

	private static void NGlobalUiInitializePostfix(NGlobalUi __instance)
	{
		try
		{
			InstallToggle(__instance);
			ApplyHiddenState(__instance.RelicInventory);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Relic visibility toggle install failed: {ex.Message}", 2);
		}
	}

	private static void NCombatUiVisiblePostfix(NCombatUi __instance)
	{
		try
		{
			_drawPileAnchor = __instance.DrawPile;
			NGlobalUi? globalUi = NRun.Instance?.GlobalUi;
			if (globalUi == null || !GodotObject.IsInstanceValid(globalUi))
			{
				return;
			}

			InstallToggle(globalUi);
			ApplyHiddenState(globalUi.RelicInventory);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Relic visibility toggle refresh failed: {ex.Message}", 2);
		}
	}

	private static void NCombatUiHiddenPostfix()
	{
		_drawPileAnchor = null;
		RefreshToggleRootPosition();
	}

	private static void NRelicInventoryRefreshPostfix(NRelicInventory __instance)
	{
		try
		{
			ApplyHiddenState(__instance);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Relic visibility refresh failed: {ex.Message}", 2);
		}
	}

	private static bool NRelicInventoryHolderDoFlashPrefix()
	{
		return !_config.HideRelics;
	}

	private static void InstallToggle(NGlobalUi globalUi)
	{
		Control? root = FindToggleRoot(globalUi);
		Button? button = root?.GetNodeOrNull<Button>($"{ToggleColumnNodeName}/{ToggleBoxNodeName}/{ToggleButtonNodeName}");
		if (root == null || !GodotObject.IsInstanceValid(root) || button == null || !GodotObject.IsInstanceValid(button))
		{
			if (root != null && GodotObject.IsInstanceValid(root))
			{
				root.GetParent()?.RemoveChild(root);
				root.QueueFree();
			}

			root = CreateToggleRoot();
			globalUi.AddChild(root);
			globalUi.MoveChild(root, globalUi.GetChildCount() - 1);
			button = root.GetNode<Button>($"{ToggleColumnNodeName}/{ToggleBoxNodeName}/{ToggleButtonNodeName}");
		}

		button.SetPressedNoSignal(_config.HideRelics);
		UpdateToggleVisualState(root, _config.HideRelics);
		root.Visible = true;
		EnsurePositionTimer(globalUi, root);
		PositionToggleRoot(root);
		Callable.From(() => PositionToggleRoot(root)).CallDeferred();
	}

	private static void OnToggleChanged(bool hideRelics)
	{
		_config.HideRelics = hideRelics;
		SaveConfig(_config);
		NGlobalUi? globalUi = NRun.Instance?.GlobalUi;
		if (globalUi?.GetNodeOrNull<Control>(ToggleRootNodeName) is { } root && GodotObject.IsInstanceValid(root))
		{
			UpdateToggleVisualState(root, hideRelics);
		}

		ApplyHiddenState(globalUi?.RelicInventory);
		Log.Info($"[{ModInfo.Id}][Mayhem] hide_relics={hideRelics}.");
	}

	private static Control CreateToggleRoot()
	{
		Control root = new()
		{
			Name = ToggleRootNodeName,
			CustomMinimumSize = ToggleRootSize,
			Size = ToggleRootSize,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		root.AnchorLeft = 0f;
		root.AnchorRight = 0f;
		root.AnchorTop = 0f;
		root.AnchorBottom = 0f;
		root.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		root.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

		VBoxContainer column = new()
		{
			Name = ToggleColumnNodeName,
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		column.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		column.AddThemeConstantOverride("separation", 0);
		root.AddChild(column);

		Control box = CreateToggleBox();
		column.AddChild(box);

		Label label = new()
		{
			Name = ToggleLabelNodeName,
			Text = "隐藏遗物",
			CustomMinimumSize = new Vector2(ToggleRootSize.X, 16f),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		label.AddThemeFontSizeOverride("font_size", 12);
		label.AddThemeColorOverride("font_color", new Color(0.94f, 0.88f, 0.68f, 1f));
		label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.75f));
		label.AddThemeConstantOverride("outline_size", 2);
		column.AddChild(label);

		return root;
	}

	private static Control CreateToggleBox()
	{
		Control box = new()
		{
			Name = ToggleBoxNodeName,
			CustomMinimumSize = ToggleBoxSize,
			MouseFilter = Control.MouseFilterEnum.Pass
		};

		Control visuals = CreateTickboxVisuals();
		box.AddChild(visuals);

		Button button = new()
		{
			Name = ToggleButtonNodeName,
			ToggleMode = true,
			Flat = true,
			Text = string.Empty,
			TooltipText = "只隐藏顶部遗物图标，不会禁用遗物效果。",
			MouseFilter = Control.MouseFilterEnum.Stop,
			FocusMode = Control.FocusModeEnum.All
		};
		button.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		ApplyTransparentButtonStyle(button);
		button.Connect(BaseButton.SignalName.Toggled, Callable.From<bool>(OnToggleChanged));
		box.AddChild(button);

		return box;
	}

	private static Control CreateTickboxVisuals()
	{
		PackedScene scene = LoadTickboxVisualScene();
		Control visuals = scene.Instantiate<Control>();
		visuals.Name = ToggleVisualsNodeName;
		visuals.MouseFilter = Control.MouseFilterEnum.Ignore;
		visuals.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		return visuals;
	}

	private static PackedScene LoadTickboxVisualScene()
	{
		try
		{
			if (PreloadManager.Cache.ContainsKey(TickboxVisualScenePath))
			{
				return PreloadManager.Cache.GetScene(TickboxVisualScenePath);
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Failed to read cached tickbox scene; using ResourceLoader: {ex.Message}", 2);
		}

		return ResourceLoader.Load<PackedScene>(TickboxVisualScenePath, cacheMode: ResourceLoader.CacheMode.Reuse)
			?? throw new InvalidOperationException($"Could not load tickbox scene: {TickboxVisualScenePath}");
	}

	private static void ApplyTransparentButtonStyle(Button button)
	{
		StyleBoxEmpty empty = new();
		button.AddThemeStyleboxOverride("normal", empty);
		button.AddThemeStyleboxOverride("hover", empty);
		button.AddThemeStyleboxOverride("pressed", empty);
		button.AddThemeStyleboxOverride("focus", empty);
		button.AddThemeStyleboxOverride("disabled", empty);
		button.AddThemeStyleboxOverride("hover_pressed", empty);
	}

	private static Control? FindToggleRoot(NGlobalUi globalUi)
	{
		Control? direct = globalUi.GetNodeOrNull<Control>(ToggleRootNodeName);
		if (direct != null && GodotObject.IsInstanceValid(direct))
		{
			return direct;
		}

		return globalUi.FindChild(ToggleRootNodeName, recursive: true, owned: false) as Control;
	}

	private static void UpdateToggleVisualState(Control root, bool hideRelics)
	{
		string tickedPath = $"{ToggleColumnNodeName}/{ToggleBoxNodeName}/{ToggleVisualsNodeName}/Ticked";
		string notTickedPath = $"{ToggleColumnNodeName}/{ToggleBoxNodeName}/{ToggleVisualsNodeName}/NotTicked";
		if (root.GetNodeOrNull<Control>(tickedPath) is { } ticked)
		{
			ticked.Visible = hideRelics;
		}

		if (root.GetNodeOrNull<Control>(notTickedPath) is { } notTicked)
		{
			notTicked.Visible = !hideRelics;
		}
	}

	private static void EnsurePositionTimer(NGlobalUi globalUi, Control root)
	{
		if (root.GetNodeOrNull<Godot.Timer>(PositionTimerNodeName) != null)
		{
			return;
		}

		Godot.Timer timer = new()
		{
			Name = PositionTimerNodeName,
			WaitTime = 0.25,
			OneShot = false,
			Autostart = true
		};
		timer.Timeout += () =>
		{
			if (!GodotObject.IsInstanceValid(globalUi) || !GodotObject.IsInstanceValid(root))
			{
				return;
			}

			root.Visible = true;
			PositionToggleRoot(root);
		};
		root.AddChild(timer);
	}

	private static void PositionToggleRoot(Control root)
	{
		if (!GodotObject.IsInstanceValid(root) || !root.IsInsideTree())
		{
			return;
		}

		Vector2 viewportSize = root.GetViewportRect().Size;
		Vector2 position = GetFallbackPosition(viewportSize);
		if (_drawPileAnchor is { } anchor && GodotObject.IsInstanceValid(anchor) && anchor.IsInsideTree())
		{
			Rect2 anchorRect = anchor.GetGlobalRect();
			if (anchorRect.Size.X > 0f && anchorRect.Size.Y > 0f)
			{
				position = new Vector2(
					anchorRect.End.X + DrawPileGap,
					anchorRect.Position.Y + (anchorRect.Size.Y - ToggleRootSize.Y) / 2f);
			}
		}

		float maxX = MathF.Max(0f, viewportSize.X - ToggleRootSize.X);
		float maxY = MathF.Max(0f, viewportSize.Y - ToggleRootSize.Y);
		root.GlobalPosition = new Vector2(Math.Clamp(position.X, 0f, maxX), Math.Clamp(position.Y, 0f, maxY));
	}

	private static Vector2 GetFallbackPosition(Vector2 viewportSize)
	{
		return new Vector2(
			LeftFallbackPadding,
			MathF.Max(0f, viewportSize.Y - ToggleRootSize.Y - BottomFallbackPadding));
	}

	private static void RefreshToggleRootPosition()
	{
		NGlobalUi? globalUi = NRun.Instance?.GlobalUi;
		if (globalUi?.GetNodeOrNull<Control>(ToggleRootNodeName) is { } root && GodotObject.IsInstanceValid(root))
		{
			root.Visible = true;
			PositionToggleRoot(root);
		}
	}

	private static void ApplyHiddenState(NRelicInventory? inventory)
	{
		if (inventory == null || !GodotObject.IsInstanceValid(inventory))
		{
			return;
		}

		bool showRelics = !_config.HideRelics;
		foreach (NRelicInventoryHolder holder in inventory.RelicNodes)
		{
			if (holder == null || !GodotObject.IsInstanceValid(holder))
			{
				continue;
			}

			holder.Visible = showRelics;
		}
	}

	private static ModUiConfig LoadOrCreateConfig()
	{
		string configPath = GetConfigPath();
		Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
		if (!File.Exists(configPath))
		{
			ModUiConfig defaultConfig = new();
			SaveConfig(defaultConfig);
			return defaultConfig;
		}

		try
		{
			ModUiConfig? parsed = JsonSerializer.Deserialize<ModUiConfig>(File.ReadAllText(configPath), JsonOptions);
			ModUiConfig config = parsed ?? new ModUiConfig();
			SaveConfig(config);
			return config;
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Relic visibility config read failed; using defaults: {ex.Message}", 2);
			ModUiConfig config = new();
			SaveConfig(config);
			return config;
		}
	}

	private static void SaveConfig(ModUiConfig config)
	{
		string configPath = GetConfigPath();
		Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
		File.WriteAllText(configPath, JsonSerializer.Serialize(config, JsonOptions));
	}

	private static string GetConfigPath()
	{
		return Path.Combine(GetDataDirectory(), ConfigFileName);
	}

	private static string GetDataDirectory()
	{
		try
		{
			string godotUserDir = Godot.OS.GetUserDataDir();
			if (!string.IsNullOrWhiteSpace(godotUserDir))
			{
				return Path.Combine(godotUserDir, ModInfo.Id);
			}
		}
		catch
		{
			// Fall back to a normal per-user directory when Godot paths are unavailable.
		}

		string baseDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
		if (string.IsNullOrWhiteSpace(baseDir))
		{
			baseDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
		}

		return Path.Combine(baseDir, "SlayTheSpire2", ModInfo.Id);
	}

	private sealed class ModUiConfig
	{
		[JsonPropertyName("hide_relics")]
		public bool HideRelics { get; set; }
	}
}
