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
	private static bool _handlingActSelection;
	private static RunState? _handlingActSelectionRunState;

	public static void ResetActSelectionState()
	{
		_handlingActSelection = false;
		_handlingActSelectionRunState = null;
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

		if (_handlingActSelection
			&& _handlingActSelectionRunState != null
			&& !ReferenceEquals(_handlingActSelectionRunState, runState))
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection: clearing stale handling state for previous run");
			ResetActSelectionState();
		}

		Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection enter: room={runState.CurrentRoom?.GetType().Name ?? "null"} actIndex={actIndex} resolved={modifier.IsActResolved(actIndex)} handling={_handlingActSelection}");
		if (_handlingActSelection || !IsCurrentRun(runState) || actIndex < 0 || actIndex > 2 || modifier.IsActResolved(actIndex))
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection skip");
			return;
		}

		_handlingActSelection = true;
		_handlingActSelectionRunState = runState;
		bool reopenMapAfterSelection = false;
		try
		{
			if (!await WaitForSelectionBlockingOverlaysToClear(runState, actIndex, "before-map-close"))
			{
				return;
			}

			if (NMapScreen.Instance?.IsOpen == true && NGame.Instance != null)
			{
				Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection: closing map before showing selection overlay");
				NMapScreen.Instance.Close(animateOut: false);
				reopenMapAfterSelection = true;
				await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			if (!IsCurrentRun(runState))
			{
				Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection abort: run is no longer current");
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

			(HextechRarityTier rarity, MonsterHexKind monsterHex) = await ResolveActRoll(runState, modifier, actIndex);
			Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection rarity: act={actIndex} rarity={rarity}");
			Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection monsterHex: act={actIndex} hex={monsterHex}");
			MonsterHexKind? finalMonsterHex = monsterHex;
			RelicModel? monsterHexRelic = CreateMonsterHexRelic(finalMonsterHex);

			NetGameType gameType = RunManager.Instance.NetService.Type;
			if (gameType is NetGameType.Singleplayer or NetGameType.None)
			{
				foreach (Player player in runState.Players)
				{
					HashSet<ModelId> excludedIds = CreateBaseExcludedIds(modifier, player, monsterHexRelic);
					List<RelicModel> options = BuildSelectableRunesForRarity(
						player,
						rarity,
						runState,
						excludedIds,
						useEndlessTagWindow: modifier.IsEndlessLoopActive);
					HashSet<ModelId> enemyRerollExcludedIds = CreateEnemyHexRerollExcludedIds(options);
					Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection options: player={player.NetId} count={options.Count} ids={string.Join(",", options.Select(o => (o.CanonicalInstance?.Id ?? o.Id).Entry))}");
					RuneSelectionResult selection = await SelectRune(
						modifier,
						player,
						options,
						monsterHexRelic,
						new HextechEnemyHexAdjustmentOptions
						{
							InitialHex = finalMonsterHex,
							ControlsEnabled = true,
							RerollFunc = (currentHex, rerollOrdinal) => RerollEnemyHexForAct(modifier, rarity, runState, actIndex, currentHex, rerollOrdinal, enemyRerollExcludedIds)
						});
					if (!IsCurrentRun(runState))
					{
						Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection abort: selection returned for stale run");
						return;
					}
					finalMonsterHex = selection.FinalMonsterHex;
					monsterHexRelic = CreateMonsterHexRelic(finalMonsterHex);
					RelicModel selected = selection.SelectedRelic ?? options[0];
					HextechTelemetry.RecordRuneChoice(runState, actIndex, rarity, player, selection.FinalOptions, selected, selection.RerollCount);
					await RelicCmd.Obtain(selected, player);
					Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection obtained: player={player.NetId} relic={(selected.CanonicalInstance?.Id ?? selected.Id).Entry}");
				}
			}
			else
			{
				finalMonsterHex = await SelectRunesForAllPlayersMultiplayer(runState, modifier, actIndex, rarity, finalMonsterHex, monsterHexRelic);
			}
			if (!IsCurrentRun(runState))
			{
				Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection abort: run changed before resolving act");
				return;
			}

			if (finalMonsterHex.HasValue)
			{
				modifier.SetMonsterHexForAct(actIndex, finalMonsterHex.Value);
			}
			else
			{
				modifier.ClearMonsterHexForAct(actIndex);
			}
			modifier.SetActResolved(actIndex, true);
			modifier.ApplyMapModifiersToCurrentAct(nameof(HandleActSelection));
			HextechEnemyUi.Refresh(modifier);
			await modifier.ApplyToCurrentEnemiesIfNeeded();
			await PersistActSelection(runState, actIndex);
			Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection resolved: act={actIndex}");
		}
		catch (OperationCanceledException)
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection abort: selection overlay closed before choice act={actIndex}");
		}
		finally
		{
			if (reopenMapAfterSelection
				&& IsCurrentRun(runState)
				&& NMapScreen.Instance != null
				&& !NMapScreen.Instance.IsOpen)
			{
				Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection: reopening map after selection overlay");
				NMapScreen.Instance.Open();
			}

			if (ReferenceEquals(_handlingActSelectionRunState, runState))
			{
				ResetActSelectionState();
			}
			Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection exit: act={actIndex}");
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
				Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection waiting: act={actIndex} reason={reason} topOverlay={overlayName} frame={frame}");
			}

			await WaitOneFrame();
		}

		Log.Info($"[{ModInfo.Id}][Mayhem] HandleHextechActSelection abort: run changed while waiting for overlays act={actIndex} reason={reason}");
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
			Log.Info($"[{ModInfo.Id}][Mayhem] PersistActSelection: saved current run after resolving act={actIndex}");
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] PersistActSelection failed: act={actIndex} error={ex}");
		}
	}
}
