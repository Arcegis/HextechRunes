using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	private static readonly HextechActSelectionGate ActSelectionGate = new();

	public static void ResetActSelectionState()
	{
		ActSelectionGate.Reset();
	}

	public static Task HandleActStarted(HextechMayhemModifier modifier)
	{
		return HandleActSelection(modifier.ActiveRunState, modifier);
	}

	public static async Task HandleActSelection(RunState runState, HextechMayhemModifier modifier)
	{
		int actIndex = runState.CurrentActIndex;
		if (!modifier.IsActResolved(actIndex) && modifier.TryRecoverResolvedActsFromPlayerRelics(nameof(HandleActSelection)))
		{
			HextechEnemyUi.Refresh(modifier);
		}

		if (ActSelectionGate.ResetIfStaleRun(runState))
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection: clearing stale handling state for previous run");
		}

		HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection enter: room={runState.CurrentRoom?.GetType().Name ?? "null"} actIndex={actIndex} resolved={modifier.IsActResolved(actIndex)} handling={ActSelectionGate.IsHandling}");
		if (ActSelectionGate.IsHandling || !IsCurrentRun(runState) || actIndex < 0 || actIndex > 2 || modifier.IsActResolved(actIndex))
		{
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection skip");
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
				HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection: closing map before showing selection overlay");
				NMapScreen.Instance.Close(animateOut: false);
				reopenMapAfterSelection = true;
				await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			if (!IsCurrentRun(runState))
			{
				HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection abort: run is no longer current");
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

			(HextechRarityTier rarity, MonsterHexKind? monsterHex) = await ResolveActRoll(runState, modifier, actIndex);
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection rarity: act={actIndex} rarity={rarity}");
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection monsterHex: act={actIndex} hex={monsterHex}");
			IReadOnlyList<MonsterHexKind> previousMonsterHexes = modifier.GetActiveMonsterHexesBeforeAct(actIndex);
			IReadOnlyList<MonsterHexKind> newMonsterHexes = ResolveNewMonsterHexesForAct(modifier, rarity, runState, actIndex, monsterHex);
			IReadOnlyList<MonsterHexKind> finalMonsterHexes = CombineMonsterHexes(previousMonsterHexes, newMonsterHexes);
			MonsterHexKind? visibleMonsterHex = FirstMonsterHexOrNull(newMonsterHexes);
			RelicModel? monsterHexRelic = CreateMonsterHexRelic(visibleMonsterHex);
			int playerHexCount = modifier.GetPlayerHexCountForAct(actIndex);

			NetGameType gameType = RunManager.Instance.NetService.Type;
			for (int choiceOrdinal = 0; choiceOrdinal < playerHexCount; choiceOrdinal++)
			{
				bool allowEnemyHexAdjustment = choiceOrdinal == 0;
				if (gameType is NetGameType.Singleplayer or NetGameType.None)
				{
					foreach (Player player in runState.Players)
					{
						HashSet<ModelId> excludedIds = CreateBaseExcludedIds(modifier, player, finalMonsterHexes);
						List<RelicModel> options = BuildSelectableRunesForRarity(
							player,
							rarity,
							runState,
							excludedIds,
							useEndlessTagWindow: modifier.IsEndlessLoopActive);
						if (options.Count == 0)
						{
							Log.Warn($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection no options: player={player.NetId} act={actIndex} ordinal={choiceOrdinal} rarity={rarity}");
							continue;
						}

						HashSet<ModelId> enemyRerollExcludedIds = CreateEnemyHexRerollExcludedIds(options);
						HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection options: player={player.NetId} ordinal={choiceOrdinal} count={options.Count} ids={string.Join(",", options.Select(o => (o.CanonicalInstance?.Id ?? o.Id).Entry))}");
						HextechEnemyHexAdjustmentOptions? enemyHexOptions = allowEnemyHexAdjustment && finalMonsterHexes.Count > 0
							? new HextechEnemyHexAdjustmentOptions
							{
								InitialHexes = newMonsterHexes,
								ExcludedHexes = finalMonsterHexes,
								RerollLimit = modifier.MonsterHexRerollLimit,
								ControlsEnabled = newMonsterHexes.Count > 0,
								RerollFunc = newMonsterHexes.Count > 0
									? (currentHexes, slotIndex, rerollOrdinal) => RerollEnemyHexForAct(
										modifier,
										rarity,
										runState,
										actIndex,
										GetMonsterHexSlot(currentHexes, slotIndex),
										rerollOrdinal,
										CreateEnemyHexRerollExcludedIds(enemyRerollExcludedIds, currentHexes, slotIndex))
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
							HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection abort: selection returned for stale run");
							return;
						}

						if (allowEnemyHexAdjustment)
						{
							newMonsterHexes = selection.ResolvedMonsterHexes;
							finalMonsterHexes = CombineMonsterHexes(previousMonsterHexes, newMonsterHexes);
							visibleMonsterHex = FirstMonsterHexOrNull(newMonsterHexes);
							monsterHexRelic = CreateMonsterHexRelic(visibleMonsterHex);
						}

						RelicModel selected = selection.SelectedRelic ?? options[0];
						HextechTelemetry.RecordRuneChoice(runState, actIndex, rarity, player, selection.FinalOptions, selected, selection.RerollCount, choiceOrdinal);
						await RelicCmd.Obtain(selected, player);
						HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection obtained: player={player.NetId} ordinal={choiceOrdinal} relic={(selected.CanonicalInstance?.Id ?? selected.Id).Entry}");
					}
				}
				else
				{
					finalMonsterHexes = await SelectRunesForAllPlayersMultiplayer(
						runState,
						modifier,
						actIndex,
						rarity,
						allowEnemyHexAdjustment ? previousMonsterHexes : finalMonsterHexes,
						allowEnemyHexAdjustment ? newMonsterHexes : [],
						monsterHexRelic,
						choiceOrdinal);
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
				HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection skipped player choices: act={actIndex} configuredPlayerHexCount={playerHexCount}");
			}
			if (!IsCurrentRun(runState))
			{
				HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection abort: run changed before resolving act");
				return;
			}

			modifier.SetMonsterHexesForAct(actIndex, finalMonsterHexes);
			modifier.SetActResolved(actIndex, true);
			modifier.ApplyMapModifiersToCurrentAct(nameof(HandleActSelection));
			HextechEnemyUi.Refresh(modifier);
			await modifier.ApplyToCurrentEnemiesIfNeeded();
			await PersistActSelection(runState, actIndex);
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection resolved: act={actIndex}");
		}
		catch (OperationCanceledException)
		{
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection abort: selection overlay closed before choice act={actIndex}");
		}
		finally
		{
			if (reopenMapAfterSelection
				&& IsCurrentRun(runState)
				&& NMapScreen.Instance != null
				&& !NMapScreen.Instance.IsOpen)
			{
				HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection: reopening map after selection overlay");
				NMapScreen.Instance.Open();
			}

			ActSelectionGate.ExitIfCurrent(runState);
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection exit: act={actIndex}");
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
				HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection waiting: act={actIndex} reason={reason} topOverlay={overlayName} frame={frame}");
			}

			await WaitOneFrame();
		}

		HextechLog.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection abort: run changed while waiting for overlays act={actIndex} reason={reason}");
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
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] PersistActSelection: saved current run after resolving act={actIndex}");
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] PersistActSelection failed: act={actIndex} error={ex}");
		}
	}
}
