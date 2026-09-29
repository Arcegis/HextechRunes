using System.Diagnostics.CodeAnalysis;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;

namespace HextechRunes;

internal static class HextechGameOverCompatibilityHooks
{
	private static NScoreLine CreateFallbackScoreLine(string label, string score, Texture2D? icon)
	{
		NScoreLine line = new()
		{
			Name = "HextechFallbackScoreLine",
			CustomMinimumSize = new Vector2(720f, 44f),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Modulate = new Color(1f, 1f, 1f, 0f)
		};

		HBoxContainer row = new()
		{
			Name = "HextechFallbackScoreLineRow",
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		row.AddThemeConstantOverride("separation", 14);
		line.AddChild(row);

		if (icon != null)
		{
			row.AddChild(new TextureRect
			{
				Name = "Icon",
				Texture = icon,
				CustomMinimumSize = new Vector2(32f, 32f),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				MouseFilter = Control.MouseFilterEnum.Ignore
			});
		}

		MegaLabel labelNode = CreateLabel("Label", label, HorizontalAlignment.Left, expand: true);
		MegaLabel scoreNode = CreateLabel("Score", score, HorizontalAlignment.Right, expand: false);
		row.AddChild(labelNode);
		row.AddChild(scoreNode);
		return line;
	}

	private static MegaLabel CreateLabel(string name, string text, HorizontalAlignment alignment, bool expand)
	{
		MegaLabel label = new()
		{
			Name = name,
			HorizontalAlignment = alignment,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsHorizontal = expand ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ShrinkEnd,
			MinFontSize = 16,
			MaxFontSize = 28,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		HextechUiTheme.ApplyDefaultMegaLabelTheme(label);
		label.SetTextAutoSize(text);
		return label;
	}

	/// <summary>
	/// 只兜住 NScoreLine.Create 自身按类型取场景节点失败的情形：InvalidCastException 由 Godot 的
	/// PackedScene.Instantiate&lt;T&gt; / Node.GetNode&lt;T&gt; 抛出（记分行场景根或 %Label/%Score/%Icon 节点类型不符；
	/// 小泛型方法被内联时抛出点显示为 NScoreLine 自身，它也是 Node），且本局启用了海克斯。其他来源的类型转换失败（第三方补丁、别的代码路径）、以及未启用海克斯的对局一律原样抛出。
	/// </summary>
	internal static bool IsScoreLineSceneTypeMismatch([NotNullWhen(true)] Exception? exception)
	{
		if (exception is not InvalidCastException invalidCast)
		{
			return false;
		}

		Type? thrower = invalidCast.TargetSite?.DeclaringType;
		bool fromGodotTypedNodeLookup = thrower != null
			&& (typeof(PackedScene).IsAssignableFrom(thrower) || typeof(Node).IsAssignableFrom(thrower));
		return fromGodotTypedNodeLookup
			&& HextechMayhemModifier.IsEnabledForRun(RunManager.Instance.DebugOnlyGetState());
	}

	[HarmonyPatch(typeof(NScoreLine), nameof(NScoreLine.Create), typeof(string), typeof(string), typeof(Texture2D))]
	[HextechPatch("compat.game-over-score-line", "结算记分行兼容", Optional = true)]
	private static class ScoreLineCreatePatch
	{
		[HarmonyFinalizer]
		private static Exception? Finalizer(
			string label,
			string score,
			Texture2D? icon,
			ref NScoreLine __result,
			Exception? __exception)
		{
			if (!IsScoreLineSceneTypeMismatch(__exception))
			{
				return __exception;
			}

			__result = CreateFallbackScoreLine(label, score, icon);
			if (HextechRunLogBudget.TryConsume("compat.game-over-score-line-fallback", 5))
			{
				HextechLog.Warn("Mayhem", $"Game over score line fallback used: {__exception.GetType().Name}: {__exception.Message}");
			}

			return null;
		}
	}
}
