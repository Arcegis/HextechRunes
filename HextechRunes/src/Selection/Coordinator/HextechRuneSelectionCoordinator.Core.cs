using Godot;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Saves;
using static HextechRunes.HextechRunePoolBuilder;
using static HextechRunes.HextechSelectionHelpers;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	// 等待遮挡界面关闭时,每 120 帧(约 2 秒)记一次日志,避免逐帧刷屏。
	private const int OverlayWaitLogIntervalFrames = 120;

	private static readonly HextechActSelectionGate ActSelectionGate = new();

	public static void ResetActSelectionState()
	{
		ActSelectionGate.Reset();
	}

	public static Task HandleActSelection(RunState runState, HextechMayhemModifier modifier)
	{
		return HandleStageSelection(runState, modifier, modifier.GetCurrentActSelectionIndex());
	}

	public static async Task HandleStageSelection(RunState runState, HextechMayhemModifier modifier, int actIndex)
	{
		if (!modifier.IsStageResolved(actIndex) && modifier.TryRecoverResolvedActsFromPlayerRelics(nameof(HandleStageSelection), actIndex))
		{
			HextechEnemyUi.Refresh(modifier);
		}

		if (ActSelectionGate.ResetIfStaleRun(runState))
		{
			HextechLog.Warn("Mayhem", $"HandleHextechActSelection: clearing stale handling state for previous run");
		}

		HextechLog.Info("Mayhem", $"HandleHextechActSelection enter: room={runState.CurrentRoom?.GetType().Name ?? "null"} actIndex={actIndex} resolved={modifier.IsStageResolved(actIndex)} handling={ActSelectionGate.IsHandling}");
		if (ActSelectionGate.IsHandling || !IsCurrentRun(runState) || actIndex < 0 || modifier.IsStageResolved(actIndex))
		{
			HextechLog.Info("Mayhem", $"HandleHextechActSelection skip");
			return;
		}

		if (HextechPresetChallengeRegistry.IsActive(runState)
			&& !HextechPresetChallengeRegistry.TryGetActPlan(runState, actIndex, out _))
		{
			await CompleteStageAsync(runState, modifier, actIndex, modifier.GetActiveMonsterHexesBeforeAct(actIndex));
			HextechLog.Info("Challenge", $"Skipped acquisition after the three preset acts: act={actIndex}");
			return;
		}

		ActSelectionGate.Enter(runState);
		bool reopenMapAfterSelection = false;
		try
		{
			if (!await WaitForSelectionBlockingOverlaysToClear(runState, actIndex, "before-map-close"))
			{
				return;
			}

			reopenMapAfterSelection = CloseMapForSelection();
			if (reopenMapAfterSelection && NGame.Instance is NGame game)
			{
				await game.ToSignal(game.GetTree(), SceneTree.SignalName.ProcessFrame);
			}

			if (!IsCurrentRun(runState))
			{
				HextechLog.Info("Mayhem", $"HandleHextechActSelection abort: run is no longer current");
				return;
			}
			if (!await WaitForSelectionBlockingOverlaysToClear(runState, actIndex, "before-selection"))
			{
				return;
			}

			foreach (Player player in runState.Players)
			{
				RemoveRunesFromGrabBags(player);
			}

			(HextechRarityTier rarity, MonsterHexKind? monsterHex, int playerHexCount) = await ResolveActRoll(runState, modifier, actIndex);
			// 必须先完成房主配置同步，再决定是否生成内容；禁用分支不能先抽取
			// 多个敌方海克斯再丢弃，否则仍会推进原版 Niche 随机流。
			if (modifier.FreezeModActiveForRunAndCheckDisabled())
			{
				modifier.SetMonsterHexesForAct(actIndex, []);
				modifier.SetStageResolved(actIndex, true);
				HextechEnemyUi.Refresh(modifier);
				await PersistActSelection(runState, actIndex);
				HextechLog.Info("Mayhem", $"Skipped content generation for disabled run: act={actIndex}");
				return;
			}

			HextechLog.Info("Mayhem", $"HandleHextechActSelection rarity: act={actIndex} rarity={rarity}");
			HextechLog.Info("Mayhem", $"HandleHextechActSelection monsterHex: act={actIndex} hex={monsterHex}");
			StageMonsterHexes stageHexes = new(modifier.GetActiveMonsterHexesBeforeAct(actIndex));
			stageHexes.SetNew(ResolveNewMonsterHexesForAct(modifier, rarity, runState, actIndex, monsterHex));
			if (!await RunStagePlayerSelectionsAsync(runState, modifier, actIndex, rarity, playerHexCount, stageHexes))
			{
				return;
			}

			if (!IsCurrentRun(runState))
			{
				HextechLog.Info("Mayhem", $"HandleHextechActSelection abort: run changed before resolving act");
				return;
			}

			await CompleteStageAsync(runState, modifier, actIndex, stageHexes.Final);
			HextechLog.Info("Mayhem", $"HandleHextechActSelection resolved: act={actIndex}");
		}
		catch (OperationCanceledException)
		{
			HextechLog.Info("Mayhem", $"HandleHextechActSelection abort: selection overlay closed before choice act={actIndex}");
		}
		catch (Exception ex)
		{
			// 记录异常，避免它继续打断开局/进幕的调用链。这里不回滚已经完成的状态写入；
			// 只有尚未 SetStageResolved 的阶段才会在后续进入或读档时重新尝试选择。
			// 捕获异常本身不保证两端恢复一致，联机状态仍由原版校验。
			HextechLog.Error("Mayhem", $"HandleHextechActSelection failed act={actIndex} networkMp={HextechRelicBase.IsNetworkMultiplayerRun()}: {ex}");
		}
		finally
		{
			// 地图 UI 是表现层,它抛错不能阻断闸门释放;否则本局后续所有进入都会被 IsHandling 挡住。
			try
			{
				if (reopenMapAfterSelection
					&& IsCurrentRun(runState)
					&& NMapScreen.Instance != null
					&& !NMapScreen.Instance.IsOpen)
				{
					HextechLog.Info("Mayhem", $"HandleHextechActSelection: reopening map after selection overlay");
					NMapScreen.Instance.Open();
				}
			}
			catch (Exception ex)
			{
				HextechLog.Error("Mayhem", $"HandleHextechActSelection: reopening map failed act={actIndex}: {ex}");
			}
			finally
			{
				ActSelectionGate.ExitIfCurrent(runState);
			}

			HextechLog.Info("Mayhem", $"HandleHextechActSelection exit: act={actIndex}");
		}
	}

	/// <summary>本幕敌方海克斯的显示/结算状态:前几幕累积 + 本幕新增,以及选择界面上展示的本幕首个新增海克斯。</summary>
	private sealed class StageMonsterHexes(IReadOnlyList<MonsterHexKind> previous)
	{
		public IReadOnlyList<MonsterHexKind> Previous { get; } = previous;
		public IReadOnlyList<MonsterHexKind> New { get; private set; } = [];
		public IReadOnlyList<MonsterHexKind> Final { get; private set; } = previous;
		public RelicModel? VisibleRelic { get; private set; }

		public void SetNew(IReadOnlyList<MonsterHexKind> newHexes)
		{
			New = newHexes;
			Final = CombineMonsterHexes(Previous, newHexes);
			VisibleRelic = CreateMonsterHexRelic(FirstMonsterHexOrNull(newHexes));
		}

		// 联机批次直接返回本幕最终累积集;允许调整敌方时再据此回推本幕新增与展示图标。
		public void SetFinalFromMultiplayer(IReadOnlyList<MonsterHexKind> finalHexes, bool enemyHexesMayHaveChanged)
		{
			Final = finalHexes;
			if (enemyHexesMayHaveChanged)
			{
				New = finalHexes
					.Where(hex => !Previous.Contains(hex))
					.ToArray();
				VisibleRelic = CreateMonsterHexRelic(FirstMonsterHexOrNull(New));
			}
		}
	}

	/// <summary>
	/// 本幕的玩家选择(每名玩家 playerHexCount 次;没有玩家选择时可能只调整敌方海克斯)。
	/// 返回 false 表示对局已切换,调用方应直接放弃本幕。
	/// </summary>
	private static async Task<bool> RunStagePlayerSelectionsAsync(
		RunState runState,
		HextechMayhemModifier modifier,
		int actIndex,
		HextechRarityTier rarity,
		int playerHexCount,
		StageMonsterHexes stageHexes)
	{
		NetGameType gameType = RunManager.Instance.NetService.Type;
		// 候选池一旦抽空,本幕后续几次选择也必然为空:每名玩家本幕只弹一次"继续"界面。
		HashSet<ulong> playersNotifiedNoOptions = [];
		for (int choiceOrdinal = 0; choiceOrdinal < playerHexCount; choiceOrdinal++)
		{
			bool allowEnemyHexAdjustment = choiceOrdinal == 0
				&& !HextechPresetChallengeRegistry.IsActive(runState);
			if (HextechPlayerContextHelper.IsSinglePlayerFlow(gameType))
			{
				foreach (Player player in runState.Players)
				{
					if (!await SelectSinglePlayerRuneAsync(runState, modifier, actIndex, rarity, choiceOrdinal, allowEnemyHexAdjustment, player, stageHexes, playersNotifiedNoOptions))
					{
						return false;
					}
				}
			}
			else
			{
				IReadOnlyList<MonsterHexKind> finalMonsterHexes = await SelectRunesForAllPlayersMultiplayer(
					runState,
					modifier,
					actIndex,
					rarity,
					stageHexes.Previous,
					stageHexes.New,
					stageHexes.VisibleRelic,
					choiceOrdinal,
					allowEnemyHexAdjustment,
					playersNotifiedNoOptions);
				stageHexes.SetFinalFromMultiplayer(finalMonsterHexes, allowEnemyHexAdjustment);
			}
		}

		if (playerHexCount <= 0)
		{
			if (NeedsEnemyOnlySelection(playerHexCount, stageHexes.New.Count, HextechPresetChallengeRegistry.IsActive(runState)))
			{
				stageHexes.SetNew(await SelectEnemyHexesOnly(runState, modifier, actIndex, rarity, stageHexes.Previous, stageHexes.New));
			}
			HextechLog.Info("Mayhem", $"HandleHextechActSelection skipped player choices: act={actIndex} configuredPlayerHexCount={playerHexCount}");
		}

		return true;
	}

	/// <summary>单机一名玩家的一次符文选择并发放。返回 false 表示对局已切换。</summary>
	private static async Task<bool> SelectSinglePlayerRuneAsync(
		RunState runState,
		HextechMayhemModifier modifier,
		int actIndex,
		HextechRarityTier rarity,
		int choiceOrdinal,
		bool allowEnemyHexAdjustment,
		Player player,
		StageMonsterHexes stageHexes,
		HashSet<ulong> playersNotifiedNoOptions)
	{
		HashSet<ModelId> excludedIds = CreateBaseExcludedIds(modifier, player);
		List<RelicModel> options = BuildSelectableRunesForRarity(
			player,
			rarity,
			runState,
			excludedIds,
			useEndlessTagWindow: modifier.IsEndlessLoopActive);
		if (options.Count == 0)
		{
			HextechLog.Warn("Mayhem", $"HandleHextechActSelection no options: player={player.NetId} act={actIndex} ordinal={choiceOrdinal} rarity={rarity}");
			// 本稀有度已无可选:仍要给玩家一个界面交代,并保留本幕敌方海克斯的调整机会。
			if (!playersNotifiedNoOptions.Add(player.NetId))
			{
				return true;
			}

			if (allowEnemyHexAdjustment && stageHexes.New.Count > 0)
			{
				stageHexes.SetNew(await SelectEnemyHexesOnly(runState, modifier, actIndex, rarity, stageHexes.Previous, stageHexes.New,
					new LocString("relic_collection", "HEXTECH_NO_RUNE_OPTIONS_TITLE").GetRawText()));
			}
			else
			{
				await ShowNoRuneOptionsScreenAsync();
			}

			if (!IsCurrentRun(runState))
			{
				HextechLog.Info("Mayhem", $"HandleHextechActSelection abort: no-options screen returned for stale run");
				return false;
			}

			return true;
		}

		HextechLog.Info("Mayhem", $"HandleHextechActSelection options: player={player.NetId} ordinal={choiceOrdinal} count={options.Count} ids={string.Join(",", options.Select(o => o.CanonicalId().Entry))}");
		// choiceOrdinal>0(!allowEnemyHexAdjustment):敌方 hex 已在第一次选择时定妥,后续玩家符文选择只读展示
		// 【本幕新增】的敌方 hex(New 在首次选择后已更新为调整后的结果),不给控件。
		// 不能用 Final:它含前几幕累积集,会把历史敌方海克斯一起显示(玩家实报)。
		HextechEnemyHexAdjustmentOptions? enemyHexOptions = stageHexes.Final.Count > 0
			? CreateEnemyHexAdjustmentOptions(
				modifier,
				rarity,
				runState,
				actIndex,
				stageHexes.New,
				controlsEnabled: allowEnemyHexAdjustment && stageHexes.New.Count > 0,
				syncContext: null,
				CancellationToken.None)
			: null;
		RuneSelectionResult selection = await SelectRune(
			modifier,
			player,
			actIndex,
			choiceOrdinal,
			options,
			stageHexes.VisibleRelic,
			enemyHexOptions);
		if (!IsCurrentRun(runState))
		{
			HextechLog.Info("Mayhem", $"HandleHextechActSelection abort: selection returned for stale run");
			return false;
		}

		if (allowEnemyHexAdjustment)
		{
			stageHexes.SetNew(selection.ResolvedMonsterHexes);
		}

		RelicModel selected = RequireCompletedSelection(
			selection.SelectedRelic,
			$"singleplayer act={actIndex} ordinal={choiceOrdinal} player={player.NetId}");
		HextechTelemetry.RecordRuneChoice(runState, actIndex, rarity, player, selection.FinalOptions, selected, selection.RerollCount, choiceOrdinal);
		modifier.CommitCharacterRuneWeight(player, selection.FinalOptions);
		await RelicCmd.Obtain(selected, player);
		HextechLog.Info("Mayhem", $"HandleHextechActSelection obtained: player={player.NetId} ordinal={choiceOrdinal} relic={selected.CanonicalId().Entry}");
		return true;
	}

	// 关掉地图才能显示选择覆盖层;返回 true 表示关过地图,选择结束后要重新打开(调用方等一帧再继续)。
	private static bool CloseMapForSelection()
	{
		if (NMapScreen.Instance?.IsOpen != true || NGame.Instance == null)
		{
			return false;
		}

		HextechLog.Info("Mayhem", $"HandleHextechActSelection: closing map before showing selection overlay");
		NMapScreen.Instance.Close(animateOut: false);
		return true;
	}

	/// <summary>本幕结算完成:写入敌方海克斯、标记已结算、应用地图修正、刷新敌方 UI 并作用到当前敌人,最后存档。</summary>
	private static async Task CompleteStageAsync(RunState runState, HextechMayhemModifier modifier, int actIndex, IReadOnlyList<MonsterHexKind> finalMonsterHexes)
	{
		modifier.SetMonsterHexesForAct(actIndex, finalMonsterHexes);
		modifier.SetStageResolved(actIndex, true);
		modifier.ApplyMapModifiersToCurrentAct(nameof(HandleStageSelection), actIndex);
		HextechEnemyUi.Refresh(modifier);
		await modifier.ApplyToCurrentEnemiesIfNeeded();
		await PersistActSelection(runState, actIndex);
	}

	private static async Task<bool> WaitForSelectionBlockingOverlaysToClear(RunState runState, int actIndex, string reason)
	{
		for (int frame = 0; IsCurrentRun(runState); frame++)
		{
			object? topOverlay = NOverlayStack.Instance?.Peek();
			if (!IsSelectionBlockingOverlay(topOverlay, out string overlayName))
			{
				return true;
			}

			if (frame % OverlayWaitLogIntervalFrames == 0)
			{
				HextechLog.Info("Mayhem", $"HandleHextechActSelection waiting: act={actIndex} reason={reason} topOverlay={overlayName} frame={frame}");
			}

			await WaitOneFrame();
		}

		HextechLog.Info("Mayhem", $"HandleHextechActSelection abort: run changed while waiting for overlays act={actIndex} reason={reason}");
		return false;
	}

	private static bool IsSelectionBlockingOverlay(object? overlay, out string overlayName)
	{
		if (overlay == null || overlay is HextechRuneSelectionScreen)
		{
			overlayName = overlay?.GetType().FullName ?? "null";
			return false;
		}

		Type overlayType = overlay.GetType();
		overlayName = overlayType.FullName ?? overlayType.Name;
		if (overlay is IOverlayScreen { ScreenType: NetScreenType.Rewards } or NCardRewardSelectionScreen)
		{
			return true;
		}

		// 第三方模组的奖励类界面不一定声明 NetScreenType.Rewards,也没有可依赖的公共基类,
		// 只能按类型名兜底识别;原版奖励界面已由上面的类型判断覆盖。
		return overlayName.Contains("Reward", StringComparison.OrdinalIgnoreCase);
	}

	private static async Task WaitOneFrame()
	{
		if (NGame.Instance != null)
		{
			await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
			return;
		}

		await Task.Yield();
	}

	private static async Task PersistActSelection(RunState runState, int actIndex)
	{
		try
		{
			if (!IsCurrentRun(runState) || RunManager.Instance.NetService.Type == NetGameType.Replay)
			{
				return;
			}

			await SaveManager.Instance.SaveRun(preFinishedRoom: null, saveProgress: false);
			HextechLog.Info("Mayhem", $"PersistActSelection: saved current run after resolving act={actIndex}");
		}
		catch (Exception ex)
		{
			HextechLog.Warn("Mayhem", $"PersistActSelection failed: act={actIndex} error={ex}");
		}
	}
}
