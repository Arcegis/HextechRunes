using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace HextechRunes;

// 配置菜单的输入:符文格子的点击/触摸长按、悬浮提示、手柄焦点导航与滚动跟随。
internal static partial class HextechRuneConfigMenuHooks
{
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
			if (inputEvent.IsActionPressed("ui_accept"))
			{
				root.GetViewport()?.SetInputAsHandled();
				ToggleRune(entry.Id, binding, pendingDisabledIds, updateSummary);
				return;
			}

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
				TaskHelper.RunSafely(ShowTouchHoverTipAfterDelay(root, entry.Relic, GetEnemyHexKind(entry), currentToken, () => pointerToken == currentToken && pointerPressed && !pointerDragged, () => longPressShown = true));
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

	private static void AttachRelicHoverTips(Control holder, RelicModel relic, MonsterHexKind? monsterHex = null)
	{
		holder.MouseEntered += () => ShowRelicHoverTips(holder, relic, monsterHex);
		holder.MouseExited += () => NHoverTipSet.Remove(holder);
		holder.FocusEntered += () => ShowRelicHoverTips(holder, relic, monsterHex);
		holder.FocusExited += () => NHoverTipSet.Remove(holder);
		holder.TreeExiting += () => NHoverTipSet.Remove(holder);
	}

	private static void ConfigureHorizontalFocus(IReadOnlyList<Button> buttons)
	{
		for (int i = 0; i < buttons.Count; i++)
		{
			buttons[i].FocusNeighborLeft = buttons[Math.Max(0, i - 1)].GetPath();
			buttons[i].FocusNeighborRight = buttons[Math.Min(buttons.Count - 1, i + 1)].GetPath();
		}
	}

	private static void WireControllerFocusScrolling(Node node)
	{
		if (node is Control control && control.FocusMode != Control.FocusModeEnum.None && !control.HasMeta(FocusScrollMetaKey))
		{
			control.SetMeta(FocusScrollMetaKey, true);
			control.FocusEntered += () =>
			{
				FindAncestor<ScrollContainer>(control)?.EnsureControlVisible(control);
			};

			// 手柄焦点要看得见:没有自定义焦点样式的按钮(步进按钮等)补一圈描边;鼠标玩家不会获得焦点,看不到它。
			if (control is BaseButton && !control.HasThemeStyleboxOverride("focus"))
			{
				control.AddThemeStyleboxOverride("focus", HextechControllerInput.CreateFocusRing(6));
			}
		}

		foreach (Node child in node.GetChildren())
		{
			WireControllerFocusScrolling(child);
		}
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
}
