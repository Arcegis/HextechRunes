using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;

namespace HextechRunes;

// 配置菜单的通用控件:文字、按钮、步进器、开关、卡片与页签。
internal static partial class HextechRuneConfigMenuHooks
{
	private static Label CreateLabel(string text, int fontSize, Color color)
	{
		MegaLabel label = new()
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			MinFontSize = fontSize,
			MaxFontSize = fontSize
		};
		HextechUiTheme.ApplyDefaultMegaLabelTheme(label);
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.Modulate = color;
		label.AddThemeColorOverride("font_color", Colors.White);
		label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.68f));
		label.AddThemeConstantOverride("outline_size", 2);
		label.SetTextAutoSize(text);
		return label;
	}

	/// <summary>改写标签文字;本菜单的标签都是 MegaLabel,走自适应字号。</summary>
	private static void SetLabelText(Label label, string text)
	{
		if (label is MegaLabel megaLabel)
		{
			megaLabel.SetTextAutoSize(text);
			return;
		}

		label.Text = text;
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
		HextechUiTheme.ApplyDefaultMegaLabelTheme(label);
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", fontColor);
		label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.62f));
		label.AddThemeConstantOverride("outline_size", 2);
		label.SetTextAutoSize(text);
		button.AddChild(label);
	}

	// 按钮文字由 AddCrispButtonText 加的 MegaLabel 子节点承载;直接改 Button.Text 会与其叠字。
	private static void SetActionButtonText(Button button, string text)
	{
		foreach (Node child in button.GetChildren())
		{
			if (child is MegaLabel label)
			{
				label.SetTextAutoSize(text);
				return;
			}
		}
	}

	private static Button CreateStyledActionButton(string text, bool compactLayout)
	{
		Button button = new()
		{
			Text = string.Empty,
			FocusMode = Control.FocusModeEnum.All,
			CustomMinimumSize = compactLayout ? new Vector2(112f, 34f) : new Vector2(132f, 38f),
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		ApplyButtonStyles(button, includeFocus: true);
		AddCrispButtonText(button, text, compactLayout ? 14 : 16, HextechUiTheme.ButtonText);
		return button;
	}

	private static Button CreateActionButton(string text, Action action, bool compactLayout = false)
	{
		Button button = CreateStyledActionButton(text, compactLayout);
		button.Pressed += action;
		return button;
	}

	/// <summary>
	/// 触发异步流程的按钮:点击在主线程同步启动,任务交给 <see cref="TaskHelper.RunSafely"/> 观察。
	/// 动作自己捕获预期内的失败(网络等),并在回到主线程后再更新节点。
	/// </summary>
	private static Button CreateAsyncActionButton(string text, Func<Button, Task> action, bool compactLayout = false)
	{
		Button button = CreateStyledActionButton(text, compactLayout);
		button.Pressed += () => TaskHelper.RunSafely(action(button));
		return button;
	}

	private static Button CreateStepButton(string text, bool compactLayout)
	{
		Button button = new()
		{
			Text = string.Empty,
			FocusMode = Control.FocusModeEnum.All,
			CustomMinimumSize = compactLayout ? new Vector2(34f, 32f) : new Vector2(38f, 34f),
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		// 不设 focus 样式:手柄焦点圈由 WireControllerFocusScrolling 统一补。
		ApplyButtonStyles(button, includeFocus: false);
		AddCrispButtonText(button, text, compactLayout ? 17 : 18, HextechUiTheme.ButtonText);
		return button;
	}

	/// <summary>按住步进按钮连续触发:先等一段延迟,之后按固定间隔重复,连按多次后加速。</summary>
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
			TaskHelper.RunSafely(RepeatStepAsync(button, () => pressToken == currentToken, action));
		};
		button.ButtonUp += () => pressToken++;
		button.TreeExiting += () => pressToken++;
	}

	private static async Task RepeatStepAsync(Button button, Func<bool> tokenIsCurrent, Action action)
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

	private static Control CreateNumericStepper(
		ConfigMenuContext context,
		string labelText,
		Func<int> getValue,
		Action<int> setValue,
		int step = 1,
		Func<string>? getDisplayText = null,
		Func<int, int, int>? stepValue = null)
	{
		bool compactLayout = context.CompactLayout;
		VBoxContainer root = new()
		{
			CustomMinimumSize = compactLayout ? new Vector2(150f, 58f) : new Vector2(190f, 70f),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		root.AddThemeConstantOverride("separation", compactLayout ? 3 : 5);

		Label label = CreateLabel(labelText, compactLayout ? 13 : 15, HextechUiTheme.StepperLabelText);
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
		Label number = CreateLabel(GetDisplay(), compactLayout ? 17 : 18, HextechUiTheme.NumberText);
		number.HorizontalAlignment = HorizontalAlignment.Center;
		number.VerticalAlignment = VerticalAlignment.Center;
		number.CustomMinimumSize = compactLayout ? new Vector2(44f, 32f) : new Vector2(54f, 34f);
		context.NumericBindings.Add(new NumericValueBinding(GetDisplay, number));

		Button minus = CreateStepButton("-", compactLayout);
		Button plus = CreateStepButton("+", compactLayout);
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
		ConfigMenuContext context,
		string labelText,
		Func<int> getValue,
		Action<int> setValue)
	{
		return CreateNumericStepper(
			context,
			labelText,
			getValue,
			setValue,
			getDisplayText: () => FormatRerollLimit(getValue()),
			stepValue: static (current, delta) => HextechRuneConfiguration.StepRerollLimit(current, delta));
	}

	private static string FormatRerollLimit(int value)
	{
		int clamped = HextechRuneConfiguration.ClampRerollLimit(value);
		return clamped == HextechRuneConfiguration.InfiniteRerollLimit
			? L("HEXTECH_REROLL_LIMIT_INFINITE")
			: clamped.ToString();
	}

	/// <summary>"标题 + 说明 + 一行步进器"的卡片:数量页的各项设置共用。</summary>
	private static Control CreateStepperCard(
		ConfigMenuContext context,
		string titleText,
		string descriptionText,
		bool spacedRow,
		params Control[] steppers)
	{
		bool compactLayout = context.CompactLayout;
		VBoxContainer section = CreateCardSection(titleText, null, compactLayout, out PanelContainer card);
		Label description = CreateLabel(descriptionText, compactLayout ? 13 : 14, HextechUiTheme.SectionDescriptionText);
		description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		section.AddChild(description);

		HBoxContainer row = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		if (spacedRow)
		{
			row.AddThemeConstantOverride("separation", compactLayout ? 10 : 18);
		}

		section.AddChild(row);
		foreach (Control stepper in steppers)
		{
			row.AddChild(stepper);
		}

		return card;
	}

	/// <summary>一行开关选项:左侧自绘开关,右侧标题与说明;点整行任意处都能切换。</summary>
	private static Control CreateBooleanOption(
		ConfigMenuContext context,
		string titleText,
		string descriptionText,
		Func<bool> getValue,
		Action<bool> setValue)
	{
		bool compactLayout = context.CompactLayout;
		HBoxContainer row = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		row.AddThemeConstantOverride("separation", compactLayout ? 8 : 12);

		Button toggle = CreatePillToggle(context, getValue, setValue);
		row.AddChild(toggle);
		row.AddChild(CreateOptionTextColumn(titleText, descriptionText, compactLayout));

		row.GuiInput += inputEvent =>
		{
			if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
			{
				toggle.ButtonPressed = !toggle.ButtonPressed;
				row.GetViewport()?.SetInputAsHandled();
			}
		};
		return row;
	}

	/// <summary>
	/// 自绘开关(pill):关=深钢灰轨道+旋钮居左,开=金色轨道+旋钮滑到右,圆形旋钮。替代原生 CheckBox
	/// 的默认主题图标,统一到本菜单的深蓝+金视觉语言。轨道颜色由按钮 pressed 态的 stylebox 自动切换,
	/// 旋钮位置由 ApplyVisual 回调驱动(同时覆盖用户点击与"重置默认"的 SetPressedNoSignal 刷新)。
	/// </summary>
	private static Button CreatePillToggle(ConfigMenuContext context, Func<bool> getValue, Action<bool> setValue)
	{
		bool compactLayout = context.CompactLayout;
		float trackWidth = compactLayout ? 44f : 50f;
		float trackHeight = compactLayout ? 24f : 28f;
		float knobDiameter = trackHeight - 6f;
		float knobOffX = 3f;
		float knobOnX = trackWidth - knobDiameter - 3f;
		float knobY = (trackHeight - knobDiameter) / 2f;

		Button toggle = new()
		{
			ToggleMode = true,
			Text = string.Empty,
			ButtonPressed = getValue(),
			CustomMinimumSize = new Vector2(trackWidth, trackHeight),
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			FocusMode = Control.FocusModeEnum.All,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
			SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
		};
		StylePillTrack(toggle, trackHeight);

		Panel knob = new()
		{
			CustomMinimumSize = new Vector2(knobDiameter, knobDiameter),
			Size = new Vector2(knobDiameter, knobDiameter),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		knob.AddThemeStyleboxOverride("panel", CreatePillKnobStyle(knobDiameter));
		toggle.AddChild(knob);

		// 旋钮以中心为锚做缩放回弹;位置另由 ApplyVisual 驱动。
		knob.PivotOffset = new Vector2(knobDiameter / 2f, knobDiameter / 2f);

		Tween? knobTween = null;
		void ApplyVisual(bool on, bool animate)
		{
			if (!GodotObject.IsInstanceValid(knob))
			{
				return;
			}

			Vector2 target = new(on ? knobOnX : knobOffX, knobY);
			if (knobTween != null && knobTween.IsValid())
			{
				knobTween.Kill();
			}

			knobTween = null;
			if (!animate || !knob.IsInsideTree())
			{
				// 首次构建(尚未进入场景树)或重置为默认时不做动画,直接落位。
				knob.Position = target;
				knob.Scale = Vector2.One;
				return;
			}

			// 开关切换:旋钮滑到目标位并带轻微过冲,同时做一次"按压回弹"缩放,手感更明确。
			knobTween = knob.CreateTween();
			knobTween.SetParallel(true);
			knobTween.TweenProperty(knob, "position", target, ToggleKnobSlideSeconds)
				.SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Back);
			knobTween.TweenProperty(knob, "scale", Vector2.One, ToggleKnobSlideSeconds)
				.From(new Vector2(0.78f, 0.78f))
				.SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Back);
		}

		ApplyVisual(getValue(), animate: false);

		toggle.Toggled += value =>
		{
			setValue(value);
			ApplyVisual(value, animate: true);
		};
		context.BooleanBindings.Add(new BooleanValueBinding(getValue, toggle, value => ApplyVisual(value, animate: true)));
		return toggle;
	}

	private static Control CreateOptionTextColumn(string titleText, string descriptionText, bool compactLayout)
	{
		VBoxContainer textColumn = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		textColumn.AddThemeConstantOverride("separation", compactLayout ? 2 : 4);
		Label title = CreateLabel(titleText, compactLayout ? 14 : 16, new Color(0.96f, 0.92f, 0.78f, 0.98f));
		title.HorizontalAlignment = HorizontalAlignment.Left;
		textColumn.AddChild(title);
		Label description = CreateLabel(descriptionText, compactLayout ? 12 : 13, HextechUiTheme.OptionDescriptionText);
		description.HorizontalAlignment = HorizontalAlignment.Left;
		description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		textColumn.AddChild(description);
		return textColumn;
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
			bool value = binding.GetValue();
			binding.Toggle.SetPressedNoSignal(value);
			binding.ApplyVisual?.Invoke(value);
		}
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

	private static PanelContainer CreateCard(out MarginContainer body, Color? accent, bool compactLayout)
	{
		PanelContainer card = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		card.AddThemeStyleboxOverride("panel", CreateCardStyle(accent));

		body = new MarginContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		int horizontal = compactLayout ? 14 : 20;
		int vertical = compactLayout ? 10 : 16;
		body.AddThemeConstantOverride("margin_left", horizontal);
		body.AddThemeConstantOverride("margin_right", horizontal);
		body.AddThemeConstantOverride("margin_top", vertical);
		body.AddThemeConstantOverride("margin_bottom", vertical);
		card.AddChild(body);
		return card;
	}

	private static VBoxContainer CreateCardSection(string title, Color? accent, bool compactLayout, out PanelContainer card)
	{
		card = CreateCard(out MarginContainer body, accent, compactLayout);
		VBoxContainer column = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		column.AddThemeConstantOverride("separation", compactLayout ? 8 : 12);
		body.AddChild(column);
		if (!string.IsNullOrEmpty(title))
		{
			column.AddChild(CreateSectionHeader(title, compactLayout ? 18 : 20));
		}

		return column;
	}

	private static Label CreateSectionHeader(string text, int fontSize)
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

	private static Label CreatePoolGroupHeader(string text, bool compactLayout)
	{
		Label label = CreateLabel(text, compactLayout ? 18 : 21, new Color(0.96f, 0.92f, 0.82f, 0.98f));
		label.CustomMinimumSize = new Vector2(0f, (compactLayout ? 18 : 21) + 6f);
		return label;
	}

	private static Button CreateTabButton(string text, Action action, bool compactLayout)
	{
		Button button = new()
		{
			Text = string.Empty,
			CustomMinimumSize = GetTabButtonSize(compactLayout),
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			FocusMode = Control.FocusModeEnum.All
		};
		AddCrispButtonText(button, text, compactLayout ? 14 : 16, HextechUiTheme.ButtonText);
		button.Pressed += action;
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
		button.AddThemeStyleboxOverride("normal", CreateTabSegmentStyle(active, false));
		button.AddThemeStyleboxOverride("hover", CreateTabSegmentStyle(active, true));
		button.AddThemeStyleboxOverride("pressed", CreateTabSegmentStyle(active, true));
		button.AddThemeStyleboxOverride("focus", HextechControllerInput.CreateFocusRing());
		if (button.GetChildCount() > 0 && button.GetChild(0) is Label label)
		{
			label.Modulate = active
				? new Color(1f, 0.86f, 0.5f, 1f)
				: new Color(0.78f, 0.82f, 0.88f, 0.86f);
		}
	}

	private static void AnimatePageIn(Control page)
	{
		if (!GodotObject.IsInstanceValid(page))
		{
			return;
		}

		// 页面由 VBoxContainer 排版定位,只做透明度动画;位移 tween 会每帧和容器排版打架。
		page.Modulate = HextechUiTheme.TransparentWhite;
		Tween tween = page.CreateTween();
		tween.TweenProperty(page, "modulate:a", 1f, PageTransitionSeconds).SetEase(Tween.EaseType.Out);
	}

	private static void AnimateTabIndicator(IReadOnlyList<Button> tabButtons, int activeIndex, bool animated)
	{
		if (activeIndex < 0 || activeIndex >= tabButtons.Count)
		{
			return;
		}

		Button active = tabButtons[activeIndex];
		if (!GodotObject.IsInstanceValid(active)
			|| active.GetParent()?.GetParent() is not Control holder
			|| holder.GetNodeOrNull<ColorRect>(TabIndicatorName) is not { } indicator)
		{
			return;
		}

		// 等排版落定后再按激活页签相对 holder 的矩形定位。
		Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(active) || !GodotObject.IsInstanceValid(indicator))
			{
				return;
			}

			float targetX = active.Position.X;
			float targetWidth = active.Size.X > 0f ? active.Size.X : indicator.Size.X;
			float targetY = active.Position.Y + active.Size.Y - 3f;
			Vector2 targetPos = new(targetX, targetY);
			Vector2 targetSize = new(targetWidth, 3f);
			if (!animated || indicator.Size.X <= 0f)
			{
				indicator.Position = targetPos;
				indicator.Size = targetSize;
				return;
			}

			Tween tween = indicator.CreateTween();
			tween.SetParallel(true);
			tween.TweenProperty(indicator, "position", targetPos, TabIndicatorSlideSeconds)
				.SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Cubic);
			tween.TweenProperty(indicator, "size", targetSize, TabIndicatorSlideSeconds)
				.SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Cubic);
		}).CallDeferred();
	}
}
