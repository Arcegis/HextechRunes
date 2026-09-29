using Godot;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace HextechRunes;

// 符文池/锻造页的图标网格:格子、图标、启用状态与点击切换。
internal static partial class HextechRuneConfigMenuHooks
{
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
		return new CenterContainer
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
		root.FocusEntered += () => root.SelfModulate = new Color(1.12f, 1.12f, 1.12f, 1f);
		root.FocusExited += () => root.SelfModulate = Colors.White;
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

	/// <summary>符文格子的图标分帧创建(每帧 <see cref="RuneConfigIconsPerFrame"/> 个),打开菜单不卡顿。</summary>
	private static async Task PopulateRuneIconsAsync(ConfigMenuContext context)
	{
		Control overlay = context.Overlay;
		if (!await HextechGodotAsync.AwaitProcessFrameAsync(overlay))
		{
			return;
		}

		int loadedThisFrame = 0;
		foreach (RuneConfigLoadTarget target in context.LoadTargets)
		{
			if (!GodotObject.IsInstanceValid(overlay) || !overlay.IsInsideTree())
			{
				return;
			}

			RuneIconBinding binding = CreateRuneIcon(target.Entry, target.PendingDisabledIds, context.UpdateSummary);
			target.Bindings.Add(binding);
			target.Grid.AddChild(binding.Root);
			WireControllerFocusScrolling(binding.Root);

			loadedThisFrame++;
			if (loadedThisFrame < RuneConfigIconsPerFrame)
			{
				continue;
			}

			loadedThisFrame = 0;
			if (!await HextechGodotAsync.AwaitProcessFrameAsync(overlay))
			{
				return;
			}
		}
	}

	private static Label CreateRuneNameLabel(string text)
	{
		Label label = CreateLabel(text, 13, new Color(0.96f, 0.97f, 1f, 1f));
		label.CustomMinimumSize = new Vector2(RuneConfigCellWidth, 38f);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Top;
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.ClipText = true;
		label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.82f));
		label.AddThemeConstantOverride("outline_size", 2);
		return label;
	}

	private static void UpdateAllRuneIcons(IReadOnlyList<RuneIconBinding> bindings, IReadOnlySet<string> pendingDisabledIds)
	{
		foreach (RuneIconBinding binding in bindings)
		{
			ApplyRuneIconState(binding, !pendingDisabledIds.Contains(binding.Id));
		}
	}

	private static void ApplyRuneIconState(RuneIconBinding binding, bool enabled, bool animated = false)
	{
		Color holderTarget = enabled
			? Colors.White
			: new Color(0.34f, 0.36f, 0.4f, 0.44f);
		Color titleTarget = enabled
			? Colors.White
			: new Color(0.6f, 0.64f, 0.72f, 0.58f);

		if (!animated || !GodotObject.IsInstanceValid(binding.Root) || !binding.Root.IsInsideTree())
		{
			binding.Holder.Modulate = holderTarget;
			binding.Title.Modulate = titleTarget;
			return;
		}

		Tween tween = binding.Root.CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(binding.Holder, "modulate", holderTarget, RuneStateFadeSeconds).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(binding.Title, "modulate", titleTarget, RuneStateFadeSeconds).SetEase(Tween.EaseType.Out);
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

		ApplyRuneIconState(binding, !pendingDisabledIds.Contains(id), animated: true);
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
}
