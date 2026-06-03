using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.sts2.Core.Nodes.TopBar;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechEnemyUi
{
	private const string EnemyHexRootName = "HextechEnemyHexStrip";
	private const string EnemyHexPanelName = "HextechEnemyHexPanel";
	private const string EnemyHexIconsName = "HextechEnemyHexIcons";
	private const string EnemyHexHolderNamePrefix = "EnemyHex-";
	private const int EnemyHexSeparation = 2;
	private const float EnemyHexScale = 0.72f;

	private static readonly FieldInfo? ModifiersContainerField = TryGetField(
		typeof(NTopBar),
		"_modifiersContainer",
		BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo? TopBarModifierModelField = TryGetField(
		typeof(NTopBarModifier),
		"_modifier",
		BindingFlags.Instance | BindingFlags.NonPublic);

	private static bool _reportedMissingTopBarMembers;

	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(NRelicBasicHolder), "OnFocus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
			prefix: new HarmonyMethod(typeof(HextechEnemyUi), nameof(EnemyHexHolderOnFocusPrefix)));
		harmony.Patch(
			RequireMethod(typeof(NRelicBasicHolder), "OnUnfocus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
			prefix: new HarmonyMethod(typeof(HextechEnemyUi), nameof(EnemyHexHolderOnUnfocusPrefix)));
	}

	public static void Refresh(HextechMayhemModifier modifier)
	{
		Control? container = GetModifiersContainer();
		if (container == null)
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] EnemyUi.Refresh: no modifiers container");
			return;
		}

		HideMayhemModifierBadge();

		IReadOnlyList<MonsterHexKind> activeHexes = modifier.GetActiveMonsterHexes();
		if (activeHexes.Count == 0)
		{
			RemoveAllEnemyHexStrips(container);
			UpdateContainerVisibility(container);
			Log.Info($"[{ModInfo.Id}][Mayhem] EnemyUi.Refresh: no active enemy hexes");
			return;
		}
		Log.Info($"[{ModInfo.Id}][Mayhem] EnemyUi.Refresh: active={string.Join(",", activeHexes)}");

		HBoxContainer strip = GetOrCreateStrip(container);
		if (!IsStripCurrent(strip, activeHexes))
		{
			RebuildStrip(strip, activeHexes);
		}

		UpdateContainerVisibility(container);
	}

	public static bool IsTopBarReady()
	{
		return GetModifiersContainer() != null;
	}

	public static void Clear()
	{
		Control? container = GetModifiersContainer();
		if (container == null)
		{
			return;
		}

		RemoveAllEnemyHexStrips(container);
		HideMayhemModifierBadge();
		UpdateContainerVisibility(container);
	}

	public static void HideMayhemModifierBadge()
	{
		Control? container = GetModifiersContainer();
		if (container == null)
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] EnemyUi.HideMayhemModifierBadge: no modifiers container");
			return;
		}

		foreach (Node child in container.GetChildren())
		{
			if (child is NTopBarModifier topBarModifier
				&& TopBarModifierModelField != null
				&& TopBarModifierModelField.GetValue(topBarModifier) is HextechMayhemModifier)
			{
				Log.Info($"[{ModInfo.Id}][Mayhem] EnemyUi.HideMayhemModifierBadge: removed top bar modifier badge");
				topBarModifier.QueueFree();
			}
		}
	}

	private static Control? GetModifiersContainer()
	{
		FieldInfo? modifiersContainerField = ModifiersContainerField;
		if (!HasTopBarMembers() || modifiersContainerField == null)
		{
			return null;
		}

		NTopBar? topBar = NRun.Instance?.GlobalUi?.TopBar;
		return topBar == null ? null : modifiersContainerField.GetValue(topBar) as Control;
	}

	private static bool HasTopBarMembers()
	{
		if (ModifiersContainerField != null && TopBarModifierModelField != null)
		{
			return true;
		}

		if (!_reportedMissingTopBarMembers)
		{
			List<string> missing = [];
			if (ModifiersContainerField == null)
			{
				missing.Add("NTopBar._modifiersContainer");
			}

			if (TopBarModifierModelField == null)
			{
				missing.Add("NTopBarModifier._modifier");
			}

			Log.Warn($"[{ModInfo.Id}][Mayhem] EnemyUi disabled: missing {string.Join(", ", missing)}.");
			_reportedMissingTopBarMembers = true;
		}

		return false;
	}

	private static HBoxContainer GetOrCreateStrip(Control container)
	{
		HBoxContainer? existingStrip = null;
		foreach (Node child in container.GetChildren())
		{
			if (child.Name != EnemyHexRootName)
			{
				continue;
			}

			if (existingStrip == null
				&& child is MarginContainer existingRoot
				&& existingRoot.GetChildCount() > 0
				&& existingRoot.GetChild(0) is PanelContainer existingPanel
				&& existingPanel.GetChildCount() > 0
				&& existingPanel.GetChild(0) is HBoxContainer existingIcons)
			{
				existingStrip = existingIcons;
				continue;
			}

			container.RemoveChild(child);
			child.QueueFree();
		}

		if (existingStrip != null)
		{
			Node? existingRootNode = existingStrip.GetParent()?.GetParent();
			if (existingRootNode != null)
			{
				container.MoveChild(existingRootNode, container.GetChildCount() - 1);
			}

			return existingStrip;
		}

		MarginContainer root = new()
		{
			Name = EnemyHexRootName,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		PanelContainer panel = new()
		{
			Name = EnemyHexPanelName,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		panel.AddThemeStyleboxOverride("panel", CreateEnemyHexStripStyle());

		HBoxContainer strip = new()
		{
			Name = EnemyHexIconsName,
			Alignment = BoxContainer.AlignmentMode.Begin,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		strip.AddThemeConstantOverride("separation", EnemyHexSeparation);

		panel.AddChild(strip);
		root.AddChild(panel);
		container.AddChild(root);
		container.MoveChild(root, container.GetChildCount() - 1);
		return strip;
	}

	private static StyleBoxFlat CreateEnemyHexStripStyle()
	{
		StyleBoxFlat style = new();
		style.BgColor = new Color(0.035f, 0.045f, 0.07f, 0.72f);
		style.BorderColor = new Color(0.36f, 0.42f, 0.52f, 0.24f);
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(10);
		style.ContentMarginLeft = 8;
		style.ContentMarginRight = 8;
		style.ContentMarginTop = 6;
		style.ContentMarginBottom = 0;
		return style;
	}

	private static void RebuildStrip(HBoxContainer strip, IReadOnlyList<MonsterHexKind> activeHexes)
	{
		foreach (Node child in strip.GetChildren())
		{
			if (child is Control control)
			{
				NHoverTipSet.Remove(control);
			}

			strip.RemoveChild(child);
			child.QueueFree();
		}

		foreach (MonsterHexKind hex in activeHexes)
		{
			Control holder = CreateEnemyHexHolder(hex);
			strip.AddChild(holder);
		}
	}

	private static void RemoveAllEnemyHexStrips(Control container)
	{
		foreach (Node child in container.GetChildren())
		{
			if (child.Name == EnemyHexRootName)
			{
				container.RemoveChild(child);
				child.QueueFree();
			}
		}
	}

	private static void UpdateContainerVisibility(Control container)
	{
		container.Visible = container.GetChildren().Any(static child => !child.IsQueuedForDeletion());
	}

	private static Control CreateEnemyHexHolder(MonsterHexKind hex)
	{
		RelicModel relic = MonsterHexCatalog.GetIconRelicForMonsterHex(hex).ToMutable();
		NRelicBasicHolder holder = NRelicBasicHolder.Create(relic)
			?? throw new InvalidOperationException("Failed to create top bar enemy hex holder.");
		holder.Name = $"{EnemyHexHolderNamePrefix}{hex}";
		holder.Scale = Vector2.One * EnemyHexScale;
		holder.MouseFilter = Control.MouseFilterEnum.Stop;
		holder.TreeExiting += () => NHoverTipSet.Remove(holder);
		return holder;
	}

	private static bool EnemyHexHolderOnFocusPrefix(NRelicBasicHolder __instance)
	{
		NHoverTipSet.Remove(__instance);

		if (!TryGetHexFromHolder(__instance, out MonsterHexKind hex))
		{
			return true;
		}

		ShowEnemyHexHoverTip(__instance, hex);
		return false;
	}

	private static bool EnemyHexHolderOnUnfocusPrefix(NRelicBasicHolder __instance)
	{
		if (!TryGetHexFromHolder(__instance, out _))
		{
			return true;
		}

		NHoverTipSet.Remove(__instance);
		return false;
	}

	private static bool TryGetHexFromHolder(Control holder, out MonsterHexKind hex)
	{
		string name = holder.Name.ToString();
		if (name.StartsWith(EnemyHexHolderNamePrefix, StringComparison.Ordinal)
			&& Enum.TryParse(name[EnemyHexHolderNamePrefix.Length..], out hex))
		{
			return true;
		}

		hex = default;
		return false;
	}

	private static void ShowEnemyHexHoverTip(Control holder, MonsterHexKind hex)
	{
		NHoverTipSet.Remove(holder);
		NHoverTipSet? hoverTipSet = NHoverTipSet.CreateAndShow(holder, MonsterHexCatalog.GetEnemyHexHoverTips(hex));
		hoverTipSet?.SetAlignment(holder, HoverTip.GetHoverTipAlignment(holder));
	}

	private static bool IsStripCurrent(HBoxContainer strip, IReadOnlyList<MonsterHexKind> activeHexes)
	{
		Godot.Collections.Array<Node> children = strip.GetChildren();
		if (children.Count != activeHexes.Count)
		{
			return false;
		}

		for (int i = 0; i < activeHexes.Count; i++)
		{
			if (children[i] is not Control control || !TryGetHexFromHolder(control, out MonsterHexKind hex) || hex != activeHexes[i])
			{
				return false;
			}
		}

		return true;
	}
}
