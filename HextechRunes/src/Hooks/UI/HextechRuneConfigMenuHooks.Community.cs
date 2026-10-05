using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace HextechRunes;

// 社区配置面板：精选(人工审核)/热门/最新/我的 四个 tab,上传当前配置、点赞、举报、删除自己的上传。
// 「应用」与导入配置码同路径:填充配置界面 pending 编辑态,「保存并关闭」生效。
// 网络请求都在按钮的同步处理器里经 TaskHelper.RunSafely 启动;异步流程自己捕获失败,
// 回到主线程(CallDeferred)后再碰节点,并先确认节点仍然有效。
internal static partial class HextechRuneConfigMenuHooks
{
	private enum CommunityTab
	{
		Featured,
		Hot,
		New,
		Mine
	}

	private sealed record CommunityDisplayEntry(
		string? Id,
		string Title,
		string Author,
		string Code,
		int Likes,
		bool IsCommunity,
		bool IsMine,
		bool Hidden);

	/// <summary>一个打开中的社区面板:列表、状态行与当前登录的 Steam 身份。</summary>
	private sealed record CommunityPanel(
		ConfigMenuContext Menu,
		Control Blocker,
		VBoxContainer List,
		Label Status,
		string MySteamId)
	{
		internal bool HasSteam => !string.IsNullOrEmpty(MySteamId);

		internal Action ReloadTab { get; set; } = static () => { };
	}

	/// <summary>上传对话框里需要在网络返回后更新的节点。</summary>
	private sealed record CommunityUploadForm(
		Control DialogBlocker,
		LineEdit TitleInput,
		Label Feedback,
		Button Confirm);

	// 本次游戏进程里点过赞的社区配置,只用于切换"点赞/取消点赞";只在主线程读写。
	private static readonly HashSet<string> SessionLikedIds = [];

	private static readonly (CommunityTab Tab, string LocKey)[] CommunityTabs =
	[
		(CommunityTab.Featured, "HEXTECH_COMMUNITY_TAB_FEATURED"),
		(CommunityTab.Hot, "HEXTECH_COMMUNITY_TAB_HOT"),
		(CommunityTab.New, "HEXTECH_COMMUNITY_TAB_NEW"),
		(CommunityTab.Mine, "HEXTECH_COMMUNITY_TAB_MINE")
	];

	/// <summary>社区 API 的排序参数;精选与"我的"走各自的接口,不用这个参数。</summary>
	private static string GetCommunitySortKey(CommunityTab tab)
	{
		return tab switch
		{
			CommunityTab.Hot => "hot",
			CommunityTab.New => "new",
			_ => throw new ArgumentOutOfRangeException(nameof(tab), tab, null)
		};
	}

	private static void OpenCommunityConfigsPanel(ConfigMenuContext menu)
	{
		bool compactLayout = menu.CompactLayout;
		Control blocker = CreateModalShell(
			dimAlpha: 0.55f,
			panelMinSize: new Vector2(compactLayout ? 540f : 680f, compactLayout ? 470f : 570f),
			horizontalMargin: compactLayout ? 16 : 22,
			verticalMargin: compactLayout ? 12 : 16,
			separation: compactLayout ? 10 : 12,
			out VBoxContainer body);
		blocker.Name = "HextechCommunityConfigsBlocker";

		Label title = CreateLabel(L("HEXTECH_CONFIG_FEATURED"), compactLayout ? 17 : 19, HextechUiTheme.DialogTitleText);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		body.AddChild(title);
		body.AddChild(CreateHairline());

		HBoxContainer tabs = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		tabs.AddThemeConstantOverride("separation", compactLayout ? 8 : 12);
		body.AddChild(tabs);

		Label status = CreateLabel(L("HEXTECH_CONFIG_FEATURED_LOADING"), 13, new Color(0.82f, 0.86f, 0.94f, 0.9f));
		status.HorizontalAlignment = HorizontalAlignment.Center;
		status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		body.AddChild(status);

		ScrollContainer scroll = new()
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		VBoxContainer list = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		list.AddThemeConstantOverride("separation", compactLayout ? 8 : 10);
		scroll.AddChild(list);
		body.AddChild(scroll);

		HextechSteamIdentity.TryGetSteamId(out string mySteamId);
		CommunityPanel panel = new(menu, blocker, list, status, mySteamId);

		HBoxContainer bottom = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		bottom.AddThemeConstantOverride("separation", compactLayout ? 10 : 14);
		Button upload = CreateActionButton(L("HEXTECH_COMMUNITY_UPLOAD"), () => OpenCommunityUploadDialog(panel), compactLayout);
		upload.Disabled = !panel.HasSteam;
		if (!panel.HasSteam)
		{
			upload.TooltipText = L("HEXTECH_COMMUNITY_NEED_STEAM");
		}

		bottom.AddChild(upload);
		bottom.AddChild(CreateActionButton(L("HEXTECH_CONFIG_CANCEL"), () => blocker.QueueFree(), compactLayout));
		body.AddChild(bottom);

		List<(CommunityTab Tab, Button Button)> tabButtons = [];
		CommunityTab currentTab = CommunityTab.Featured;
		void SelectTab(CommunityTab tab)
		{
			currentTab = tab;
			foreach ((CommunityTab buttonTab, Button button) in tabButtons)
			{
				button.Disabled = buttonTab == tab;
			}

			foreach (Node child in list.GetChildren())
			{
				child.QueueFree();
			}

			ShowCommunityStatus(status, L("HEXTECH_CONFIG_FEATURED_LOADING"));
			TaskHelper.RunSafely(PopulateCommunityListAsync(panel, tab));
		}

		panel.ReloadTab = () => SelectTab(currentTab);
		foreach ((CommunityTab tab, string locKey) in CommunityTabs)
		{
			if (tab == CommunityTab.Mine && !panel.HasSteam)
			{
				continue;
			}

			Button tabButton = CreateActionButton(L(locKey), () => SelectTab(tab), compactLayout);
			tabButtons.Add((tab, tabButton));
			tabs.AddChild(tabButton);
		}

		menu.Overlay.AddChild(blocker);
		HextechControllerOverlay.RegisterModal(blocker, tabButtons.Count > 0 ? tabButtons[0].Button : null);
		SelectTab(CommunityTab.Featured);
	}

	/// <summary>
	/// 覆盖层内的模态弹窗骨架:半透明遮罩 + 居中面板 + 内容列。调用方往 <paramref name="body"/> 里加内容,
	/// 再把返回的遮罩挂到覆盖层并用 <see cref="HextechControllerOverlay.RegisterModal"/> 登记。
	/// </summary>
	private static Control CreateModalShell(
		float dimAlpha,
		Vector2 panelMinSize,
		int horizontalMargin,
		int verticalMargin,
		int separation,
		out VBoxContainer body)
	{
		Control blocker = new()
		{
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		blocker.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		ColorRect dim = new()
		{
			Color = new Color(0f, 0f, 0f, dimAlpha),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		blocker.AddChild(dim);

		PanelContainer panel = new()
		{
			CustomMinimumSize = panelMinSize
		};
		panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.GrowHorizontal = Control.GrowDirection.Both;
		panel.GrowVertical = Control.GrowDirection.Both;
		blocker.AddChild(panel);

		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", horizontalMargin);
		margin.AddThemeConstantOverride("margin_right", horizontalMargin);
		margin.AddThemeConstantOverride("margin_top", verticalMargin);
		margin.AddThemeConstantOverride("margin_bottom", verticalMargin);
		panel.AddChild(margin);

		body = new VBoxContainer();
		body.AddThemeConstantOverride("separation", separation);
		margin.AddChild(body);
		return blocker;
	}

	private static void ShowCommunityStatus(Label status, string text)
	{
		status.Visible = true;
		SetLabelText(status, text);
	}

	private static async Task PopulateCommunityListAsync(CommunityPanel panel, CommunityTab tab)
	{
		List<CommunityDisplayEntry>? entries = await FetchCommunityEntriesAsync(panel.MySteamId, tab);

		Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(panel.Blocker) || !GodotObject.IsInstanceValid(panel.List) || !GodotObject.IsInstanceValid(panel.Status))
			{
				return;
			}

			if (entries == null)
			{
				ShowCommunityStatus(panel.Status, L("HEXTECH_CONFIG_FEATURED_ERROR"));
				return;
			}

			if (entries.Count == 0)
			{
				ShowCommunityStatus(panel.Status, L("HEXTECH_CONFIG_FEATURED_EMPTY"));
				return;
			}

			panel.Status.Visible = false;
			foreach (CommunityDisplayEntry entry in entries)
			{
				panel.List.AddChild(CreateCommunityConfigCard(panel, entry));
			}
		}).CallDeferred();
	}

	/// <summary>拉取某个页签的条目;请求失败返回 null(接口层已记日志)。</summary>
	private static async Task<List<CommunityDisplayEntry>?> FetchCommunityEntriesAsync(string mySteamId, CommunityTab tab)
	{
		if (tab == CommunityTab.Featured)
		{
			IReadOnlyList<HextechFeaturedConfigs.FeaturedConfigEntry>? featured = await HextechFeaturedConfigs.FetchAsync().ConfigureAwait(false);
			return featured?
				.Select(static entry => new CommunityDisplayEntry(
					entry.Id, entry.Name ?? string.Empty, entry.Author ?? string.Empty, entry.Code ?? string.Empty,
					-1, IsCommunity: false, IsMine: false, Hidden: false))
				.ToList();
		}

		bool mine = tab == CommunityTab.Mine;
		IReadOnlyList<HextechCommunityClient.CommunityConfigEntry>? community = mine
			? await HextechCommunityClient.FetchMineAsync(mySteamId).ConfigureAwait(false)
			: await HextechCommunityClient.FetchCommunityAsync(GetCommunitySortKey(tab)).ConfigureAwait(false);
		return community?
			.Select(entry => new CommunityDisplayEntry(
				entry.Id, entry.Title ?? string.Empty, entry.Author ?? string.Empty, entry.Code ?? string.Empty,
				entry.Likes, IsCommunity: true, IsMine: mine, Hidden: entry.Hidden))
			.ToList();
	}

	private static Control CreateCommunityConfigCard(CommunityPanel panel, CommunityDisplayEntry entry)
	{
		bool compactLayout = panel.Menu.CompactLayout;
		PanelContainer card = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		StyleBoxFlat cardStyle = CreateButtonStyle(new Color(0.1f, 0.12f, 0.17f, 0.75f), HextechUiTheme.SoftSteelBorder);
		cardStyle.ContentMarginLeft = compactLayout ? 12f : 14f;
		cardStyle.ContentMarginRight = compactLayout ? 12f : 14f;
		cardStyle.ContentMarginTop = compactLayout ? 8f : 10f;
		cardStyle.ContentMarginBottom = compactLayout ? 8f : 10f;
		card.AddThemeStyleboxOverride("panel", cardStyle);

		// 布局:左列(标题/作者 + 简介贴左下) | 右侧操作按钮竖排——简介长短不影响按钮位置。
		HBoxContainer row = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		row.AddThemeConstantOverride("separation", compactLayout ? 10 : 14);
		card.AddChild(row);
		row.AddChild(CreateCommunityCardInfo(panel, entry));
		row.AddChild(CreateCommunityCardActions(panel, entry));
		return card;
	}

	private static Control CreateCommunityCardInfo(CommunityPanel panel, CommunityDisplayEntry entry)
	{
		bool compactLayout = panel.Menu.CompactLayout;
		VBoxContainer left = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		left.AddThemeConstantOverride("separation", 4);

		HBoxContainer headerRow = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		headerRow.AddThemeConstantOverride("separation", 8);
		Label name = CreateLabel(entry.Title, compactLayout ? 16 : 18, new Color(0.96f, 0.9f, 0.7f, 1f));
		name.VerticalAlignment = VerticalAlignment.Center;
		headerRow.AddChild(name);
		if (entry.Hidden)
		{
			Label hiddenTag = CreateLabel(L("HEXTECH_COMMUNITY_HIDDEN"), 12, new Color(1f, 0.62f, 0.62f, 0.95f));
			hiddenTag.VerticalAlignment = VerticalAlignment.Center;
			headerRow.AddChild(hiddenTag);
		}

		left.AddChild(headerRow);
		if (!string.IsNullOrWhiteSpace(entry.Author))
		{
			left.AddChild(CreateLabel(entry.Author, 12, new Color(0.72f, 0.76f, 0.84f, 0.85f)));
		}

		// 占位把简介推到左下角
		Control leftSpacer = new()
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		left.AddChild(leftSpacer);

		string summaryText = BuildConfigSummaryText(entry.Code, panel.Menu.PoolIds);
		if (!string.IsNullOrEmpty(summaryText))
		{
			Label summaryLabel = CreateLabel(summaryText, 12, new Color(0.85f, 0.88f, 0.94f, 0.92f));
			summaryLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			summaryLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			summaryLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkEnd;
			left.AddChild(summaryLabel);
		}

		return left;
	}

	private static Control CreateCommunityCardActions(CommunityPanel panel, CommunityDisplayEntry entry)
	{
		bool compactLayout = panel.Menu.CompactLayout;
		VBoxContainer actions = new()
		{
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};
		actions.AddThemeConstantOverride("separation", 6);

		if (entry.IsCommunity && entry.Id is { } likeId && !entry.IsMine)
		{
			Button like = CreateAsyncActionButton(
				FormatLikes(Math.Max(0, entry.Likes)),
				button => ToggleLikeAsync(button, panel.MySteamId, likeId, entry.Likes),
				compactLayout);
			like.Disabled = !panel.HasSteam;
			actions.AddChild(like);
		}

		actions.AddChild(CreateActionButton(L("HEXTECH_CONFIG_FEATURED_APPLY"), () => ApplyCommunityConfig(panel, entry), compactLayout));

		if (entry.IsCommunity && entry.Id is { } ownId && entry.IsMine && panel.HasSteam)
		{
			actions.AddChild(CreateAsyncActionButton(
				L("HEXTECH_COMMUNITY_DELETE"),
				button => DeleteOwnConfigAsync(button, panel, ownId),
				compactLayout));
		}
		else if (entry.IsCommunity && entry.Id is { } reportId)
		{
			Button report = CreateAsyncActionButton(
				L("HEXTECH_COMMUNITY_REPORT"),
				button => ReportConfigAsync(button, panel.MySteamId, reportId),
				compactLayout);
			report.Disabled = !panel.HasSteam;
			actions.AddChild(report);
		}

		return actions;
	}

	private static string FormatLikes(int likes)
	{
		return $"♥ {likes}";
	}

	/// <summary>「应用」:解析成功填进编辑态并关闭面板;配置码无法解析时留在面板里提示。</summary>
	private static void ApplyCommunityConfig(CommunityPanel panel, CommunityDisplayEntry entry)
	{
		HextechConfigShareCodec.ImportPreview? preview = HextechConfigShareCodec.TryParse(entry.Code);
		if (preview == null)
		{
			ShowCommunityStatus(panel.Status, L("HEXTECH_COMMUNITY_APPLY_INVALID"));
			return;
		}

		ApplyImportPreview(panel.Menu, preview);
		panel.Blocker.QueueFree();
	}

	private static async Task ToggleLikeAsync(Button like, string mySteamId, string entryId, int fallbackLikes)
	{
		bool on = !SessionLikedIds.Contains(entryId);
		HextechCommunityClient.CommunityApiResult result = await HextechCommunityClient.LikeAsync(mySteamId, entryId, on);

		Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(like) || !result.Ok)
			{
				return;
			}

			if (on)
			{
				SessionLikedIds.Add(entryId);
			}
			else
			{
				SessionLikedIds.Remove(entryId);
			}

			SetActionButtonText(like, FormatLikes(result.Likes >= 0 ? result.Likes : fallbackLikes));
		}).CallDeferred();
	}

	private static async Task DeleteOwnConfigAsync(Button remove, CommunityPanel panel, string entryId)
	{
		remove.Disabled = true;
		await HextechCommunityClient.DeleteAsync(panel.MySteamId, entryId);

		// 无论成败都刷新当前页签:列表以服务器为准。
		Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(panel.Blocker))
			{
				panel.ReloadTab();
			}
		}).CallDeferred();
	}

	private static async Task ReportConfigAsync(Button report, string mySteamId, string entryId)
	{
		// 乐观 UI:点击立即置灰改字(Pressed 在主线程),网络结果不影响展示。
		report.Disabled = true;
		SetActionButtonText(report, L("HEXTECH_COMMUNITY_REPORTED"));
		await HextechCommunityClient.ReportAsync(mySteamId, entryId);
	}

	private static void OpenCommunityUploadDialog(CommunityPanel panel)
	{
		bool compactLayout = panel.Menu.CompactLayout;
		Control dialogBlocker = CreateModalShell(
			dimAlpha: 0.5f,
			panelMinSize: new Vector2(compactLayout ? 380f : 460f, 0f),
			horizontalMargin: 18,
			verticalMargin: 14,
			separation: 10,
			out VBoxContainer body);

		Label title = CreateLabel(L("HEXTECH_COMMUNITY_UPLOAD"), 16, HextechUiTheme.DialogTitleText);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		body.AddChild(title);

		Label hint = CreateLabel(L("HEXTECH_COMMUNITY_UPLOAD_HINT"), 12, HextechUiTheme.HintText);
		hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		body.AddChild(hint);

		LineEdit titleInput = new()
		{
			PlaceholderText = L("HEXTECH_COMMUNITY_TITLE_PLACEHOLDER"),
			MaxLength = 30
		};
		body.AddChild(titleInput);

		Label feedback = CreateLabel(" ", 12, new Color(1f, 0.6f, 0.6f, 0.95f));
		feedback.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		body.AddChild(feedback);

		HBoxContainer buttons = new()
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		buttons.AddThemeConstantOverride("separation", 12);
		Button confirm = CreateAsyncActionButton(
			L("HEXTECH_COMMUNITY_UPLOAD_CONFIRM"),
			button => UploadCommunityConfigAsync(panel, new CommunityUploadForm(dialogBlocker, titleInput, feedback, button)),
			compactLayout);
		buttons.AddChild(confirm);
		buttons.AddChild(CreateActionButton(L("HEXTECH_CONFIG_CANCEL"), () => dialogBlocker.QueueFree(), compactLayout));
		body.AddChild(buttons);

		panel.Blocker.AddChild(dialogBlocker);
		HextechControllerOverlay.RegisterModal(dialogBlocker, titleInput);
		titleInput.GrabFocus();
	}

	private static async Task UploadCommunityConfigAsync(CommunityPanel panel, CommunityUploadForm form)
	{
		string uploadTitle = form.TitleInput.Text.Trim();
		if (uploadTitle.Length == 0)
		{
			SetLabelText(form.Feedback, L("HEXTECH_COMMUNITY_TITLE_EMPTY"));
			return;
		}

		form.Confirm.Disabled = true;
		HextechCommunityClient.CommunityApiResult result = await HextechCommunityClient.UploadAsync(
			panel.MySteamId,
			HextechSteamIdentity.GetPersonaName(),
			uploadTitle,
			BuildPendingShareCode(panel.Menu));

		Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(form.DialogBlocker))
			{
				return;
			}

			if (result.Ok)
			{
				form.DialogBlocker.QueueFree();
				if (GodotObject.IsInstanceValid(panel.Blocker))
				{
					panel.ReloadTab();
				}

				return;
			}

			form.Confirm.Disabled = false;
			SetLabelText(form.Feedback, result.Error switch
			{
				"title_rejected" => L("HEXTECH_COMMUNITY_ERR_TITLE_REJECTED"),
				"quota_exceeded" => L("HEXTECH_COMMUNITY_ERR_QUOTA"),
				"too_frequent" or "daily_limit" => L("HEXTECH_COMMUNITY_ERR_RATE"),
				"banned" => L("HEXTECH_COMMUNITY_ERR_BANNED"),
				_ => L("HEXTECH_CONFIG_FEATURED_ERROR")
			});
		}).CallDeferred();
	}

	/// <summary>三行本地化摘要：我方海克斯 启用/总数、敌方海克斯 启用/总数、双方每幕数量。</summary>
	private static string BuildConfigSummaryText(string code, ConfigPoolIds poolIds)
	{
		HextechConfigShareCodec.ImportPreview? preview = HextechConfigShareCodec.TryParse(code);
		if (preview == null)
		{
			return string.Empty;
		}

		HextechRunConfigurationSnapshot snapshot = preview.Snapshot;
		return string.Format(
			L("HEXTECH_COMMUNITY_SUMMARY"),
			ConfigPoolIds.CountEnabled(poolIds.Player, snapshot.DisabledPlayerRuneIds),
			poolIds.Player.Count,
			ConfigPoolIds.CountEnabled(poolIds.Enemy, snapshot.DisabledMonsterHexIds),
			poolIds.Enemy.Count,
			string.Join("-", snapshot.PlayerHexCountsByAct),
			string.Join("-", snapshot.EnemyHexCountsByAct));
	}
}
