using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using static HextechRunes.HextechRunePoolBuilder;
using static HextechRunes.HextechSelectionHelpers;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	// 单人流程；联机的每幕选择统一走 SelectRuneMultiplayer。
	private static async Task<RuneSelectionResult> SelectRune(
		HextechMayhemModifier modifier,
		Player player,
		int actIndex,
		int choiceOrdinal,
		IReadOnlyList<RelicModel> options,
		RelicModel? monsterHexRelic,
		HextechEnemyHexAdjustmentOptions? enemyHexOptions = null)
	{
		HextechRuneSelectionScreen screen = await CreateLocalRuneSelectionScreenAsync(
			modifier,
			player,
			actIndex,
			choiceOrdinal,
			options,
			monsterHexRelic,
			enemyHexOptions,
			multiplayer: false,
			CancellationToken.None);
		RelicModel? selectedRelic = (await screen.RelicsSelected()).FirstOrDefault();
		return new RuneSelectionResult(selectedRelic, HextechWeightedRuneOptions.Copy(screen.CurrentRelics), screen.RerollHistory.Count, screen.CurrentMonsterHexes);
	}

	/// <summary>
	/// 本机玩家的符文选择界面(单机与联机共用):记“已见”、建金色重掷会话、推入界面。
	/// 两种流程只差重掷的随机来源——单机推进原版 Niche 随机流,联机用各端一致的稳定哈希并只在本机记已见。
	/// </summary>
	private static async Task<HextechRuneSelectionScreen> CreateLocalRuneSelectionScreenAsync(
		HextechMayhemModifier modifier,
		Player player,
		int actIndex,
		int choiceOrdinal,
		IReadOnlyList<RelicModel> options,
		RelicModel? monsterHexRelic,
		HextechEnemyHexAdjustmentOptions? enemyHexOptions,
		bool multiplayer,
		CancellationToken cancellationToken)
	{
		MarkRelicsSeen(options);
		modifier.RecordSeenPlayerRunes(player, options);
		HashSet<ModelId> seenOptionIds = CreateSeenOptionIds(options, modifier.GetSeenPlayerRuneIds(player));
		HextechGoldenRerollSession goldenReroll = CreateGoldenRerollSession(
			modifier,
			player,
			actIndex,
			choiceOrdinal,
			options);
		Func<IReadOnlyList<RelicModel>, int, int, IReadOnlyList<RelicModel>> rerollFunc = multiplayer
			? (relics, slotIndex, rerollOrdinal) => RerollSingleOptionAndTrackMultiplayer(
				modifier,
				player,
				relics,
				slotIndex,
				actIndex,
				rerollOrdinal,
				seenOptionIds,
				GetGoldenRerollOverride(goldenReroll))
			: (relics, slotIndex, rerollOrdinal) => RerollSingleOptionAndTrack(
				modifier,
				player,
				relics,
				slotIndex,
				seenOptionIds,
				GetGoldenRerollOverride(goldenReroll),
				rerollOrdinal);
		return await CreateRuneSelectionScreenAsync(
			options,
			monsterHexRelic,
			rerollFunc,
			enemyHexOptions,
			modifier.PlayerRuneRerollLimit,
			goldenRerollSession: goldenReroll,
			cancellationToken: cancellationToken,
			selfPickPool: BuildSelfPickPool(modifier, player, options));
	}

	private static async Task<RuneSelectionResult> SelectRuneMultiplayer(
		HextechMayhemModifier modifier,
		PendingRuneSelection selection,
		PlayerChoiceSynchronizer synchronizer,
		int actIndex,
		int choiceOrdinal,
		RelicModel? monsterHexRelic,
		HextechEnemyHexAdjustmentOptions? enemyHexOptions = null,
		Func<HextechRuneSelectionScreen, Task>? afterLocalSelection = null,
		Action<HextechRuneSelectionScreen>? screenCreated = null,
		Func<Task?>? getConcurrentTask = null,
		CancellationToken cancellationToken = default)
	{
		string context = $"rune-choice act={actIndex} ordinal={choiceOrdinal}";
		cancellationToken.ThrowIfCancellationRequested();
		if (selection.IsLocal)
		{
			HextechRuneSelectionScreen screen = await CreateLocalRuneSelectionScreenAsync(
				modifier,
				selection.Player,
				actIndex,
				choiceOrdinal,
				selection.Options,
				monsterHexRelic,
				enemyHexOptions,
				multiplayer: true,
				cancellationToken);
			screenCreated?.Invoke(screen);
			RelicModel? selectedRelic;
			try
			{
				Task<IEnumerable<RelicModel>> localSelection = screen.RelicsSelected(removeOverlay: false);
				selectedRelic = (await WaitForSelectionWithConcurrentFailure(
					localSelection,
					getConcurrentTask?.Invoke(),
					context,
					cancellationToken)).FirstOrDefault();
			}
			catch (OperationCanceledException)
			{
				if (HextechPlayerContextHelper.IsMultiplayerConnected())
				{
					uint canceledChoiceId = SyncLocalHextechChoice(
						synchronizer,
						selection.Player,
						selection.ChoiceId,
						CreateRuneChoiceResult(actIndex, choiceOrdinal, screen, selectedRelic: null),
						context);
					modifier.RecordSeenPlayerRunes(selection.Player, screen.CurrentRelics);
					HextechLog.Info("Mayhem", $"RuneChoice sync canceled: act={actIndex} ordinal={choiceOrdinal} player={selection.Player.NetId} choiceId={canceledChoiceId}");
				}

				throw;
			}

			uint sentChoiceId = SyncLocalHextechChoice(
				synchronizer,
				selection.Player,
				selection.ChoiceId,
				CreateRuneChoiceResult(actIndex, choiceOrdinal, screen, selectedRelic),
				context);
			// 存档里的"已见"参与双端比对：联机各端对每名玩家只记初始候选（各端同种子生成）与同步来的最终候选，
			// 重随中途刷出又被换掉的只在本机界面记为已见（见 RerollSingleOptionAndTrackMultiplayer），这里与
			// 远端 ResolveRemoteRuneChoice 的记法对齐。
			modifier.RecordSeenPlayerRunes(selection.Player, screen.CurrentRelics);
			HextechLog.Info("Mayhem", $"RuneChoice sync local: act={actIndex} ordinal={choiceOrdinal} player={selection.Player.NetId} choiceId={sentChoiceId}");
			_ = RequireCompletedSelection(
				selectedRelic,
				$"local {context} player={selection.Player.NetId} choiceId={sentChoiceId}");

			if (afterLocalSelection != null)
			{
				await afterLocalSelection(screen).WaitAsync(cancellationToken);
			}

			return new RuneSelectionResult(selectedRelic, HextechWeightedRuneOptions.Copy(screen.CurrentRelics), screen.RerollHistory.Count, screen.CurrentMonsterHexes);
		}

		HextechLog.Info("Mayhem", $"RuneChoice wait remote: act={actIndex} ordinal={choiceOrdinal} player={selection.Player.NetId} choiceId={selection.ChoiceId}");
		(PlayerChoiceResult remoteChoice, uint receivedChoiceId)? received = await TryWaitForRemoteHextechChoice(
			synchronizer,
			(RunState)selection.Player.RunState,
			selection.Player,
			selection.ChoiceId,
			result => HextechChoiceCodec.IsRuneSelection(result, actIndex, choiceOrdinal),
			context,
			RemoteRuneChoicePollFrames,
			() => ShouldKeepWaitingForRemoteRuneChoice((RunState)selection.Player.RunState),
			cancellationToken: cancellationToken);
		if (!received.HasValue)
		{
			throw new OperationCanceledException(
				$"Remote rune selection was interrupted: {context} player={selection.Player.NetId} choiceId={selection.ChoiceId}.");
		}

		(PlayerChoiceResult remoteChoice, uint receivedChoiceId) = received.Value;
		HextechLog.Info("Mayhem", $"RuneChoice remote received: act={actIndex} ordinal={choiceOrdinal} player={selection.Player.NetId} choiceId={receivedChoiceId}");
		return ResolveRemoteRuneChoice(modifier, selection.Player, actIndex, choiceOrdinal, remoteChoice);
	}

	private static bool ShouldKeepWaitingForRemoteRuneChoice(RunState runState)
	{
		return IsCurrentRun(runState) && HextechPlayerContextHelper.IsMultiplayerConnected();
	}

	private static async Task<HextechRuneSelectionScreen> CreateRuneSelectionScreenAsync(
		IReadOnlyList<RelicModel> relics,
		RelicModel? monsterHexRelic,
		Func<IReadOnlyList<RelicModel>, int, int, IReadOnlyList<RelicModel>>? rerollFunc = null,
		HextechEnemyHexAdjustmentOptions? enemyHexOptions = null,
		int playerRuneRerollLimit = 1,
		string? titleOverride = null,
		HextechGoldenRerollSession? goldenRerollSession = null,
		CancellationToken cancellationToken = default,
		IReadOnlyList<RelicModel>? selfPickPool = null,
		bool continueOnly = false)
	{
		await WaitForSingletonAsync(static () => NOverlayStack.Instance, cancellationToken: cancellationToken);
		HextechRuneSelectionScreen selectionScreen = HextechRuneSelectionScreen.Create(
			relics,
			monsterHexRelic,
			rerollFunc,
			enemyHexOptions,
			playerRuneRerollLimit,
			titleOverride,
			goldenRerollSession: goldenRerollSession,
			selfPickPool: selfPickPool,
			continueOnly: continueOnly);
		if (NOverlayStack.Instance == null)
		{
			throw new InvalidOperationException("NOverlayStack is not available for rune selection.");
		}

		NOverlayStack.Instance.Push(selectionScreen);
		enemyHexOptions?.ScreenCreated?.Invoke(selectionScreen);
		return selectionScreen;
	}

	/// <summary>
	/// 本稀有度已无任何可选海克斯(未见的和见过没选的都抽完、其余全部拥有/互斥/禁用):只给说明和"继续"按钮。
	/// 纯本机界面,不抽取、不同步、不发放;联机时各端对"无候选"的判断一致,不需要额外协议。
	/// </summary>
	private static async Task ShowNoRuneOptionsScreenAsync(CancellationToken cancellationToken = default)
	{
		HextechRuneSelectionScreen? screen = null;
		try
		{
			screen = await CreateRuneSelectionScreenAsync(
				[],
				null,
				titleOverride: new LocString(HextechRuneLabels.LocTable, "HEXTECH_NO_RUNE_OPTIONS_TITLE").GetRawText(),
				cancellationToken: cancellationToken,
				continueOnly: true);
			// 联机批次被取消(断线、换局)时不能一直等玩家点继续。
			Task completed = await Task.WhenAny(
				screen.RelicsSelected(removeOverlay: false),
				Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));
			cancellationToken.ThrowIfCancellationRequested();
			await completed;
		}
		finally
		{
			if (screen != null)
			{
				await screen.DismissAfterSelectionComplete();
			}
		}
	}

	/// <summary>
	/// 玩家海克斯重随次数为无限时,选择界面改为自选:列出本次候选稀有度的全部合法海克斯。
	/// 合法池与重随同一套规则(配置启用、版本可用、本幕允许、角色可用、已拥有与互斥排除),不排除"已见",不消耗随机数。
	/// 只在本机构造给界面用;远端只收到最终候选与选中序号,按 ID 还原,不需要这份池。
	/// </summary>
	private static IReadOnlyList<RelicModel>? BuildSelfPickPool(HextechMayhemModifier modifier, Player player, IReadOnlyList<RelicModel> options)
	{
		if (modifier.PlayerRuneRerollLimit != HextechRuneConfiguration.InfiniteRerollLimit || options.Count == 0)
		{
			return null;
		}

		try
		{
			HextechRarityTier rarity = GetRarityForOptions(options);
			List<RelicModel> pool = BuildSelectableRunePool(player, rarity, (RunState)player.RunState)
				.Select(relic => CreateSelectableRuneOption(player, relic))
				.ToList();
			return pool.Count > 0 ? pool : null;
		}
		catch (Exception ex)
		{
			// 构造失败就退回普通的三选一界面,不阻断本幕选择。
			HextechLog.Warn("Mayhem", $"Self-pick pool unavailable, falling back to regular choices: player={player.NetId} error={ex.GetType().Name}: {ex.Message}");
			return null;
		}
	}

	private static PlayerChoiceResult CreateRuneChoiceResult(int actIndex, int choiceOrdinal, HextechRuneSelectionScreen screen, RelicModel? selectedRelic)
	{
		int selectedIndex = IndexOfRelicInstance(screen.CurrentRelics, selectedRelic);
		HextechLog.Info("Mayhem", $"CreateRuneChoiceResult: act={actIndex} ordinal={choiceOrdinal} selectedIndex={selectedIndex} rerolls={string.Join(",", screen.RerollHistory)}");
		return HextechChoiceCodec.CreateRuneSelection(actIndex, choiceOrdinal, selectedIndex, screen.RerollHistory, screen.CurrentRelics);
	}

	private static RuneSelectionResult ResolveRemoteRuneChoice(
		HextechMayhemModifier modifier,
		Player player,
		int actIndex,
		int choiceOrdinal,
		PlayerChoiceResult remoteChoice)
	{
		if (!HextechChoiceCodec.TryDecodeRuneSelection(remoteChoice, actIndex, choiceOrdinal, out int selectedIndex, out List<int> rerollHistory, out List<ModelId> syncedOptionIds))
		{
			string message =
				$"Malformed rune selection payload: act={actIndex} ordinal={choiceOrdinal} " +
				$"player={player.NetId} result={remoteChoice}";
			throw CreateProtocolFailure($"rune-choice act={actIndex} ordinal={choiceOrdinal}", message);
		}

		if (syncedOptionIds.Count == 0)
		{
			string message =
				$"Rune selection payload omitted authoritative final options: act={actIndex} " +
				$"ordinal={choiceOrdinal} player={player.NetId}";
			throw CreateProtocolFailure($"rune-choice act={actIndex} ordinal={choiceOrdinal}", message);
		}

		if (!AreRegisteredPlayerRuneIds(syncedOptionIds))
		{
			string message =
				$"Rune selection payload contains non-hextech rune options: act={actIndex} ordinal={choiceOrdinal} " +
				$"player={player.NetId} ids={string.Join(",", syncedOptionIds.Select(static id => id.ToString()))}";
			throw CreateProtocolFailure($"rune-choice act={actIndex} ordinal={choiceOrdinal}", message);
		}

		if (!TryCreateSyncedRuneOptions(player, syncedOptionIds, actIndex, choiceOrdinal, out List<RelicModel> syncedOptions))
		{
			string message =
				$"Failed to load authoritative rune options: act={actIndex} ordinal={choiceOrdinal} " +
				$"player={player.NetId} ids={string.Join(",", syncedOptionIds.Select(static id => id.Entry))}";
			throw CreateProtocolFailure($"rune-choice act={actIndex} ordinal={choiceOrdinal}", message);
		}

		if (!HextechGeneratedRuneDataCodec.Restore(remoteChoice, syncedOptions))
		{
			throw CreateProtocolFailure("generated rune data", "Invalid or missing generated rune recipe.");
		}

		if (!HextechRuneWeightCodec.TryRestore(remoteChoice, syncedOptions, out syncedOptions))
		{
			throw CreateProtocolFailure("character rune weight", "Rune selection omitted a valid final character weight.");
		}

		if (selectedIndex < -1 || selectedIndex >= syncedOptions.Count)
		{
			string message =
				$"Invalid rune selection index: act={actIndex} ordinal={choiceOrdinal} player={player.NetId} " +
				$"index={selectedIndex} optionCount={syncedOptions.Count}";
			throw CreateProtocolFailure($"rune-choice act={actIndex} ordinal={choiceOrdinal}", message);
		}

		MarkRelicsSeen(syncedOptions);
		modifier.RecordSeenPlayerRunes(player, syncedOptions);
		RelicModel syncedSelectedRelic = RequireCompletedSelection(
			selectedIndex >= 0 ? syncedOptions[selectedIndex] : null,
			$"remote rune-choice act={actIndex} ordinal={choiceOrdinal} player={player.NetId}");
		HextechLog.Info("Mayhem", $"ResolveRemoteRuneChoice: player={player.NetId} selectedIndex={selectedIndex} rerolls={string.Join(",", rerollHistory)} syncedOptions={string.Join(",", syncedOptions.Select(static o => o.CanonicalId().Entry))}");
		return new RuneSelectionResult(syncedSelectedRelic, syncedOptions, rerollHistory.Count);
	}

	private static async Task<T> WaitForSelectionWithConcurrentFailure<T>(
		Task<T> selectionTask,
		Task? concurrentTask,
		string context,
		CancellationToken cancellationToken)
	{
		if (concurrentTask == null)
		{
			return await selectionTask.WaitAsync(cancellationToken);
		}

		using CancellationTokenSource monitorCancellation =
			CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		Task concurrentFailure = WaitForFailureAsync(concurrentTask, monitorCancellation.Token);
		Task<T> cancelableSelection = selectionTask.WaitAsync(cancellationToken);
		try
		{
			Task winner = await Task.WhenAny(cancelableSelection, concurrentFailure);
			if (winner == concurrentFailure)
			{
				await concurrentFailure;
			}

			return await cancelableSelection;
		}
		finally
		{
			monitorCancellation.Cancel();
			ObserveCompletion(concurrentFailure, $"{context} concurrent task monitor");
		}
	}

	private static async Task WaitForFailureAsync(Task task, CancellationToken cancellationToken)
	{
		await task.WaitAsync(cancellationToken);
		await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
	}

	/// <summary>
	/// 远端同步来的候选必须全部是已登记的海克斯玩家符文(内置或外部 API 登记):候选会原样发放给各端,
	/// 畸形或恶意载荷不能借此让各端发放任意原版遗物。稀有度不做一致性校验——金色重掷会把单个槽位升一档。
	/// </summary>
	internal static bool AreRegisteredPlayerRuneIds(IReadOnlyList<ModelId> optionIds)
	{
		return optionIds.Count > 0
			&& optionIds.All(static id => HextechCatalog.TryGetPlayerRuneRarityById(id, out _));
	}

	private static bool TryCreateSyncedRuneOptions(
		Player player,
		IReadOnlyList<ModelId> optionIds,
		int actIndex,
		int choiceOrdinal,
		out List<RelicModel> options)
	{
		options = new(optionIds.Count);
		try
		{
			foreach (ModelId id in optionIds)
			{
				RelicModel relic = ModelDb.GetById<RelicModel>(id);
				options.Add(CreateSelectableRuneOption(player, relic));
			}

			return options.Count > 0;
		}
		catch (Exception ex)
		{
			HextechLog.Error("Mayhem", $"ResolveRemoteRuneChoice: failed to load synced option model: act={actIndex} ordinal={choiceOrdinal} player={player.NetId} ids={string.Join(",", optionIds)} error={ex}");
			options.Clear();
			return false;
		}
	}
}
