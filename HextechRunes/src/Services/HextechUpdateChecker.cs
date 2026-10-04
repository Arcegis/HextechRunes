using System.Diagnostics.CodeAnalysis;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace HextechRunes;

internal static partial class HextechUpdateChecker
{
	private const string NoticeName = "HextechRunesUpdateNotice";
	private const int MaxCheckAttempts = 2;
	private const int MaxNoticeAttachAttempts = 30;
	private const string NoticeLocTable = "main_menu_ui";

	private static readonly string[] VersionEndpoints =
	[
		HextechServerEndpoints.StaticVersionEndpoint,
		HextechServerEndpoints.ApiVersionEndpoint
	];

	private static readonly object StateLock = new();
	private static Task<UpdateCheckResult>? _checkTask;
	private static UpdateCheckResult? _cachedResult;

	private enum UpdateCheckStatus
	{
		Unavailable,
		UpToDate,
		UpdateAvailable
	}

	// 只存结构化结果,文案在主线程显示时按当前语言格式化(检查在后台线程完成)。
	private sealed record UpdateCheckResult(UpdateCheckStatus Status, string CurrentVersion, string? LatestVersion, bool Cacheable);

	/// <summary>
	/// 配置菜单保存后即时同步主页版本更新说明的显隐:开则(重新)挂上,关则移除现有提示。
	/// <paramref name="contextNode"/> 只需是当前场景树里的任意有效节点(如配置浮层),用于定位主菜单。
	/// </summary>
	internal static void ApplyNoticeVisibility(Node contextNode)
	{
		if (!GodotObject.IsInstanceValid(contextNode) || contextNode.GetTree()?.Root is not { } root)
		{
			return;
		}

		if (HextechUiPreferences.ShowUpdateNotice)
		{
			if (FindMainMenu(root) is { } mainMenu)
			{
				TaskHelper.RunSafely(ShowNoticeWhenStatusLayerReadyAsync(mainMenu));
			}
		}
		else
		{
			RemoveExistingNotice(root);
		}
	}

	private static NMainMenu? FindMainMenu(Node node)
	{
		if (node is NMainMenu menu)
		{
			return menu;
		}

		foreach (Node child in node.GetChildren())
		{
			if (FindMainMenu(child) is { } found)
			{
				return found;
			}
		}

		return null;
	}

	private static async Task ShowNoticeWhenStatusLayerReadyAsync(NMainMenu mainMenu)
	{
		for (int attempt = 1; attempt <= MaxNoticeAttachAttempts; attempt++)
		{
			if (!GodotObject.IsInstanceValid(mainMenu))
			{
				return;
			}

			try
			{
				if (TryShowNotice(mainMenu, attempt))
				{
					return;
				}
			}
			catch (Exception ex)
			{
				HextechLog.Warn("Mayhem", $"Update checker UI failed: {ex.Message}");
				return;
			}

			if (!await HextechGodotAsync.AwaitProcessFrameAsync(mainMenu))
			{
				return;
			}
		}

		HextechLog.Warn("Mayhem", $"Update checker UI skipped: vanilla mod status label not found after {MaxNoticeAttachAttempts} frames.");
	}

	private static bool TryShowNotice(NMainMenu mainMenu, int attempt)
	{
		if (!TryFindNoticeLayer(mainMenu, out Node searchRoot, out Label? template, out Node? noticeHost))
		{
			if (attempt is 1 or MaxNoticeAttachAttempts || attempt % 10 == 0)
			{
				HextechLog.Info("Mayhem", $"Update checker UI waiting for vanilla mod status label: attempt={attempt} root={DescribeNode(searchRoot)}.");
			}

			return false;
		}

		ShowNotice(searchRoot, template, noticeHost);
		HextechLog.Info("Mayhem", $"Update checker UI attached: attempt={attempt} template={DescribeNode(template)} host={DescribeNode(noticeHost)} root={DescribeNode(searchRoot)} noticeIndex={template.GetIndex() + 1}.");
		return true;
	}

	internal static bool TryFindNoticeLayer(
		NMainMenu mainMenu,
		out Node searchRoot,
		[NotNullWhen(true)] out Label? template,
		[NotNullWhen(true)] out Node? noticeHost)
	{
		searchRoot = ResolveNoticeSearchRoot(mainMenu);
		template = mainMenu.GetNodeOrNull<Label>("%ModdedWarning")
			?? FindVanillaModStatusLabel(searchRoot);
		noticeHost = template?.GetParent();
		return template != null && noticeHost != null;
	}

	private static void ShowNotice(Node searchRoot, Label template, Node noticeHost)
	{
		RemoveExistingNotice(searchRoot);
		Label label = CreateNotice(template, noticeHost);
		UpdateCheckResult? cachedResult;
		Task<UpdateCheckResult> checkTask;
		lock (StateLock)
		{
			cachedResult = _cachedResult;
			if (cachedResult != null)
			{
				SetNoticeText(label, FormatNoticeText(cachedResult));
				return;
			}

			_checkTask ??= CheckLatestVersionAsync();
			checkTask = _checkTask;
		}

		TaskHelper.RunSafely(ApplyCheckResultAsync(label, checkTask));
	}

	private static Label CreateNotice(Label template, Node noticeHost)
	{
		Label label = CreateNoticeLabel(template);
		ConfigureNoticePlacement(label);
		SetNoticeText(label, new LocString(NoticeLocTable, "HEXTECH_UPDATE_CHECKING").GetFormattedText());
		noticeHost.AddChild(label);
		MoveNoticeNextToTemplate(label, template, noticeHost);
		return label;
	}

	private static void MoveNoticeNextToTemplate(Label label, Label template, Node noticeHost)
	{
		int targetIndex = Math.Min(template.GetIndex() + 1, noticeHost.GetChildCount() - 1);
		noticeHost.MoveChild(label, targetIndex);
	}

	private static Node ResolveNoticeSearchRoot(NMainMenu mainMenu)
	{
		return mainMenu.GetTree()?.Root is Node root ? root : mainMenu;
	}

	private static Label CreateNoticeLabel(Label template)
	{
		Label label = template is MegaLabel ? new MegaLabel() : new Label();
		label.Name = NoticeName;
		label.MouseFilter = Control.MouseFilterEnum.Ignore;
		label.ZIndex = template.ZIndex;
		label.ZAsRelative = template.ZAsRelative;
		ApplyNoticeStyleFromTemplate(label, template);
		return label;
	}

	private static void ApplyNoticeStyleFromTemplate(Label label, Label template)
	{
		label.Modulate = template.Modulate;
		label.SelfModulate = template.SelfModulate;
		label.Theme = template.Theme;
		label.ThemeTypeVariation = template.ThemeTypeVariation;
		Font font = template.GetThemeFont("font");
		if (font != null)
		{
			label.AddThemeFontOverride("font", font);
		}

		int fontSize = template.GetThemeFontSize("font_size");
		if (fontSize > 0)
		{
			label.AddThemeFontSizeOverride("font_size", fontSize);
		}

		label.AddThemeColorOverride("font_color", template.GetThemeColor("font_color"));
		label.AddThemeColorOverride("font_outline_color", template.GetThemeColor("font_outline_color"));
		label.AddThemeColorOverride("font_shadow_color", template.GetThemeColor("font_shadow_color"));
		label.AddThemeConstantOverride("outline_size", template.GetThemeConstant("outline_size"));
		label.AddThemeConstantOverride("shadow_offset_x", template.GetThemeConstant("shadow_offset_x"));
		label.AddThemeConstantOverride("shadow_offset_y", template.GetThemeConstant("shadow_offset_y"));
	}

	private static void ConfigureNoticePlacement(Label label)
	{
		label.HorizontalAlignment = HorizontalAlignment.Left;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.AnchorLeft = 0f;
		label.AnchorTop = 1f;
		label.AnchorRight = 0f;
		label.AnchorBottom = 1f;
		label.OffsetLeft = 16f;
		label.OffsetTop = -44f;
		label.OffsetRight = 760f;
		label.OffsetBottom = -14f;
	}

	private static Label? FindVanillaModStatusLabel(Node root)
	{
		foreach (Node child in root.GetChildren())
		{
			if (child.Name == NoticeName)
			{
				continue;
			}

			if (child is Label label
				&& (child.Name == "ModdedWarning" || IsVanillaModStatusText(label.Text)))
			{
				return label;
			}

			Label? nested = FindVanillaModStatusLabel(child);
			if (nested != null)
			{
				return nested;
			}
		}

		return null;
	}

	private static bool IsVanillaModStatusText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		string normalized = text.Trim();
		if (MatchesLocalizedTemplate(
			normalized,
			new LocString(NoticeLocTable, "MODDED_WARNING").GetRawText()))
		{
			return true;
		}

		// 兜底的文本猜测:只在按节点名(%ModdedWarning / ModdedWarning)与原版本地化模板都找不到状态标签时才用到,
		// 覆盖场景节点改名或本地化表尚未载入的版本。原版这条标签没有专用类型可判定,只能看文本。
		if (normalized.Contains("模组", StringComparison.Ordinal) && normalized.Contains("已加载", StringComparison.Ordinal))
		{
			return true;
		}

		return normalized.Contains("mod", StringComparison.OrdinalIgnoreCase)
			&& normalized.Contains("loaded", StringComparison.OrdinalIgnoreCase);
	}

	private static bool MatchesLocalizedTemplate(string text, string template)
	{
		if (string.IsNullOrWhiteSpace(template))
		{
			return false;
		}

		int textCursor = 0;
		int templateCursor = 0;
		bool matchedLiteral = false;
		while (templateCursor < template.Length)
		{
			int placeholderStart = template.IndexOf('{', templateCursor);
			int literalEnd = placeholderStart >= 0 ? placeholderStart : template.Length;
			string literal = template[templateCursor..literalEnd].Trim();
			if (literal.Length > 0)
			{
				int match = text.IndexOf(literal, textCursor, StringComparison.Ordinal);
				if (match < 0)
				{
					return false;
				}

				textCursor = match + literal.Length;
				matchedLiteral = true;
			}

			if (placeholderStart < 0)
			{
				break;
			}

			int placeholderEnd = template.IndexOf('}', placeholderStart + 1);
			if (placeholderEnd < 0)
			{
				return false;
			}

			templateCursor = placeholderEnd + 1;
		}

		return matchedLiteral;
	}

	private static void SetNoticeText(Label label, string text)
	{
		if (label is MegaLabel megaLabel)
		{
			megaLabel.SetTextAutoSize(text);
			return;
		}

		label.Text = text;
	}

	private static void RemoveExistingNotice(Node mainMenu)
	{
		foreach (Node child in mainMenu.GetChildren())
		{
			if (child.Name == NoticeName)
			{
				child.QueueFree();
				continue;
			}

			RemoveExistingNotice(child);
		}
	}

	private static string DescribeNode(Node? node)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return "<null>";
		}

		try
		{
			return $"{node.GetType().Name}:{node.GetPath()}";
		}
		catch
		{
			return node.GetType().Name;
		}
	}

	private static string FormatNoticeText(UpdateCheckResult result)
	{
		LocString text = result.Status switch
		{
			UpdateCheckStatus.UpdateAvailable => new LocString(NoticeLocTable, "HEXTECH_UPDATE_AVAILABLE"),
			UpdateCheckStatus.UpToDate => new LocString(NoticeLocTable, "HEXTECH_UPDATE_LATEST"),
			_ => new LocString(NoticeLocTable, "HEXTECH_UPDATE_UNAVAILABLE")
		};
		text.Add("Current", result.CurrentVersion);
		text.Add("Latest", result.LatestVersion ?? "");
		return text.GetFormattedText();
	}

	private static async Task ApplyCheckResultAsync(Label label, Task<UpdateCheckResult> checkTask)
	{
		// 不加 ConfigureAwait(false):从主线程发起,续体回到 Godot 主线程,再按当前语言格式化文案。
		UpdateCheckResult result = await checkTask;
		if (!result.Cacheable)
		{
			lock (StateLock)
			{
				if (ReferenceEquals(_checkTask, checkTask))
				{
					_checkTask = null;
				}
			}
		}

		if (GodotObject.IsInstanceValid(label))
		{
			string text = FormatNoticeText(result);
			if (label is MegaLabel)
			{
				label.CallDeferred(nameof(MegaLabel.SetTextAutoSize), text);
			}
			else
			{
				label.CallDeferred("set", "text", text);
			}
		}
	}

	[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready), new Type[0])]
	[HextechPatch("service.update-checker", "更新检查")]
	private static class MainMenuReadyPatch
	{
		[HarmonyPostfix]
		private static void Postfix(NMainMenu __instance)
		{
			// 由「配置-杂项」里的本地开关控制是否在主页左下角显示版本更新说明(默认开)。
			if (!HextechUiPreferences.ShowUpdateNotice)
			{
				return;
			}

			TaskHelper.RunSafely(ShowNoticeWhenStatusLayerReadyAsync(__instance));
		}
	}
}
