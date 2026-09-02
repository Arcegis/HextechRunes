using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static partial class HextechRuneConfigMenuHooks
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
	private const float OverlayOpenSeconds = 0.16f;
	private const float OverlayCloseSeconds = 0.12f;
	private const float OverlayOpenScale = 0.965f;
	private const float PageTransitionSeconds = 0.13f;
	private const float TabIndicatorSlideSeconds = 0.16f;
	private const float RuneStateFadeSeconds = 0.12f;
	private const float ToggleKnobSlideSeconds = 0.17f;
	private const string ConfigPanelName = "HextechRuneConfigPanel";
	private const string TabIndicatorName = "HextechRuneConfigTabIndicator";
	private static readonly FieldInfo? MainMenuButtonLocStringField = TryGetField(typeof(NMainMenuTextButton), "_locString");
	private static readonly FieldInfo? MainMenuLastHitButtonField = TryGetField(typeof(NMainMenu), "_lastHitButton");
	private static readonly MethodInfo? MainMenuButtonFocusedMethod = TryGetMethod(typeof(NMainMenu), "MainMenuButtonFocused", BindingFlags.Instance | BindingFlags.NonPublic, typeof(NMainMenuTextButton));
	private static readonly MethodInfo? MainMenuButtonUnfocusedMethod = TryGetMethod(typeof(NMainMenu), "MainMenuButtonUnfocused", BindingFlags.Instance | BindingFlags.NonPublic, typeof(NMainMenuTextButton));


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

			if (!await HextechGodotAsync.AwaitProcessFrameAsync(mainMenu))
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
			HextechLog.Info($"[{ModInfo.Id}][RuneConfig] Main menu config button attached.");
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
		ConfigureNativeMenuNeighbors(buttonHost, configButton, settingsButton);
		ConnectNativeMenuButton(configButton);
		return true;
	}

	private static void ConfigureNativeMenuNeighbors(Control buttonHost, NMainMenuTextButton configButton, NMainMenuTextButton settingsButton)
	{
		Control configControl = configButton;
		Control settingsControl = settingsButton;
		configControl.FocusNeighborTop = settingsControl.GetPath();
		settingsControl.FocusNeighborBottom = configControl.GetPath();

		int nextIndex = configButton.GetIndex() + 1;
		if (nextIndex < buttonHost.GetChildCount() && buttonHost.GetChild(nextIndex) is Control nextControl)
		{
			configControl.FocusNeighborBottom = nextControl.GetPath();
			nextControl.FocusNeighborTop = configControl.GetPath();
		}
		else
		{
			configControl.FocusNeighborBottom = configControl.GetPath();
		}
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

	[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready), new Type[0])]
	[HextechPatch("ui.rune-config-menu", "海克斯配置菜单")]
	private static class MainMenuReadyPatch
	{
		[HarmonyPostfix]
		private static void Postfix(NMainMenu __instance)
		{
			TaskHelper.RunSafely(AttachButtonWhenReadyAsync(__instance));
		}
	}
}
