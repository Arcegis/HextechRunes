using Godot;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Saves;
using static HextechRunes.HextechSelectionHelpers;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
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
			IReadOnlyList<MonsterHexKind> activeMonsterHexes = modifier.GetActiveMonsterHexesBeforeAct(actIndex);
			modifier.SetMonsterHexesForAct(actIndex, activeMonsterHexes);
			modifier.SetStageResolved(actIndex, true);
			modifier.ApplyMapModifiersToCurrentAct(nameof(HandleStageSelection), actIndex);
			HextechEnemyUi.Refresh(modifier);
			await modifier.ApplyToCurrentEnemiesIfNeeded();
			await PersistActSelection(runState, actIndex);
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

			if (NMapScreen.Instance?.IsOpen == true && NGame.Instance != null)
			{
				HextechLog.Info("Mayhem", $"HandleHextechActSelection: closing map before showing selection overlay");
				NMapScreen.Instance.Close(animateOut: false);
				reopenMapAfterSelection = true;
				await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
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
			IReadOnlyList<MonsterHexKind> previousMonsterHexes = modifier.GetActiveMonsterHexesBeforeAct(actIndex);
			IReadOnlyList<MonsterHexKind> newMonsterHexes = ResolveNewMonsterHexesForAct(modifier, rarity, runState, actIndex, monsterHex);
			IReadOnlyList<MonsterHexKind> finalMonsterHexes = CombineMonsterHexes(previousMonsterHexes, newMonsterHexes);
			MonsterHexKind? visibleMonsterHex = FirstMonsterHexOrNull(newMonsterHexes);
			RelicModel? monsterHexRelic = CreateMonsterHexRelic(visibleMonsterHex);
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
								continue;
							}

							if (allowEnemyHexAdjustment && newMonsterHexes.Count > 0)
							{
								newMonsterHexes = await SelectEnemyHexesOnly(runState, modifier, actIndex, rarity, previousMonsterHexes, newMonsterHexes,
									new LocString("relic_collection", "HEXTECH_NO_RUNE_OPTIONS_TITLE").GetRawText());
								finalMonsterHexes = CombineMonsterHexes(previousMonsterHexes, newMonsterHexes);
								visibleMonsterHex = FirstMonsterHexOrNull(newMonsterHexes);
								monsterHexRelic = CreateMonsterHexRelic(visibleMonsterHex);
							}
							else
							{
								await ShowNoRuneOptionsScreenAsync();
							}

							if (!IsCurrentRun(runState))
							{
								HextechLog.Info("Mayhem", $"HandleHextechActSelection abort: no-options screen returned for stale run");
								return;
							}

							continue;
						}

						HashSet<MonsterHexKind> seenEnemyHexes = modifier.GetKnownMonsterHexes().ToHashSet();
						seenEnemyHexes.UnionWith(newMonsterHexes);
						HextechLog.Info("Mayhem", $"HandleHextechActSelection options: player={player.NetId} ordinal={choiceOrdinal} count={options.Count} ids={string.Join(",", options.Select(o => (o.CanonicalId()).Entry))}");
						// choiceOrdinal>0(!allowEnemyHexAdjustment):敌方 hex 已在第一次选择时定妥,后续玩家符文选择只读展示
						// 【本幕新增】的敌方 hex(newMonsterHexes 在首次选择后已更新为调整后的结果),不给控件。
						// 不能用 finalMonsterHexes:它含前几幕累积集,会把历史敌方海克斯一起显示(玩家实报)。
						HextechEnemyHexAdjustmentOptions? enemyHexOptions = finalMonsterHexes.Count > 0
							? new HextechEnemyHexAdjustmentOptions
							{
								InitialHexes = newMonsterHexes,
								RerollLimit = modifier.MonsterHexRerollLimit,
								ControlsEnabled = allowEnemyHexAdjustment && newMonsterHexes.Count > 0,
								RerollFunc = allowEnemyHexAdjustment && newMonsterHexes.Count > 0
									? (currentHexes, slotIndex, rerollOrdinal) => RerollEnemyHexForAct(
										modifier,
										rarity,
										runState,
										actIndex,
										GetMonsterHexSlot(currentHexes, slotIndex),
										rerollOrdinal,
										CreateEnemyHexRerollExcludedIds(currentHexes, slotIndex),
										seenEnemyHexes)
									: null
							}
							: null;
						RuneSelectionResult selection = await SelectRune(
							modifier,
							player,
							actIndex,
							choiceOrdinal,
							options,
							monsterHexRelic,
							enemyHexOptions);
						if (!IsCurrentRun(runState))
						{
							HextechLog.Info("Mayhem", $"HandleHextechActSelection abort: selection returned for stale run");
							return;
						}

						if (allowEnemyHexAdjustment)
						{
							newMonsterHexes = selection.ResolvedMonsterHexes;
							finalMonsterHexes = CombineMonsterHexes(previousMonsterHexes, newMonsterHexes);
							visibleMonsterHex = FirstMonsterHexOrNull(newMonsterHexes);
							monsterHexRelic = CreateMonsterHexRelic(visibleMonsterHex);
						}

						RelicModel selected = RequireCompletedSelection(
							selection.SelectedRelic,
							$"singleplayer act={actIndex} ordinal={choiceOrdinal} player={player.NetId}");
						HextechTelemetry.RecordRuneChoice(runState, actIndex, rarity, player, selection.FinalOptions, selected, selection.RerollCount, choiceOrdinal);
						modifier.CommitCharacterRuneWeight(player, selection.FinalOptions);
						await RelicCmd.Obtain(selected, player);
						HextechLog.Info("Mayhem", $"HandleHextechActSelection obtained: player={player.NetId} ordinal={choiceOrdinal} relic={(selected.CanonicalId()).Entry}");
					}
				}
				else
				{
					finalMonsterHexes = await SelectRunesForAllPlayersMultiplayer(
						runState,
						modifier,
						actIndex,
						rarity,
						previousMonsterHexes,
						newMonsterHexes,
						monsterHexRelic,
						choiceOrdinal,
						allowEnemyHexAdjustment,
						playersNotifiedNoOptions);
					if (allowEnemyHexAdjustment)
					{
						newMonsterHexes = finalMonsterHexes
							.Where(hex => !previousMonsterHexes.Contains(hex))
							.ToArray();
						visibleMonsterHex = FirstMonsterHexOrNull(newMonsterHexes);
						monsterHexRelic = CreateMonsterHexRelic(visibleMonsterHex);
					}
				}
			}

			if (playerHexCount <= 0)
			{
				if (NeedsEnemyOnlySelection(playerHexCount, newMonsterHexes.Count, HextechPresetChallengeRegistry.IsActive(runState)))
				{
					newMonsterHexes = await SelectEnemyHexesOnly(runState, modifier, actIndex, rarity, previousMonsterHexes, newMonsterHexes);
					finalMonsterHexes = CombineMonsterHexes(previousMonsterHexes, newMonsterHexes);
				}
				HextechLog.Info("Mayhem", $"HandleHextechActSelection skipped player choices: act={actIndex} configuredPlayerHexCount={playerHexCount}");
			}
			if (!IsCurrentRun(runState))
			{
				HextechLog.Info("Mayhem", $"HandleHextechActSelection abort: run changed before resolving act");
				return;
			}

			modifier.SetMonsterHexesForAct(actIndex, finalMonsterHexes);
			modifier.SetStageResolved(actIndex, true);
			modifier.ApplyMapModifiersToCurrentAct(nameof(HandleStageSelection), actIndex);
			HextechEnemyUi.Refresh(modifier);
			await modifier.ApplyToCurrentEnemiesIfNeeded();
			await PersistActSelection(runState, actIndex);
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

	private static async Task<bool> WaitForSelectionBlockingOverlaysToClear(RunState runState, int actIndex, string reason)
	{
		for (int frame = 0; IsCurrentRun(runState); frame++)
		{
			object? topOverlay = NOverlayStack.Instance?.Peek();
			if (!IsSelectionBlockingOverlay(topOverlay, out string overlayName))
			{
				return true;
			}

			if (frame % 120 == 0)
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
		if (overlay is IOverlayScreen { ScreenType: NetScreenType.Rewards })
		{
			return true;
		}

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

			await SaveManager.Instance.SaveRun(null!, saveProgress: false);
			HextechLog.Info("Mayhem", $"PersistActSelection: saved current run after resolving act={actIndex}");
		}
		catch (Exception ex)
		{
			HextechLog.Warn("Mayhem", $"PersistActSelection failed: act={actIndex} error={ex}");
		}
	}
}
