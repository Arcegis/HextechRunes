using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	private static async Task SynchronizeActSelectionApplied(RunState runState, PlayerChoiceSynchronizer synchronizer, int actIndex)
	{
		RunManager runManager = RunManager.Instance;
		List<Task> pendingAcks = [];
		foreach (Player player in runState.Players)
		{
				uint choiceId = synchronizer.ReserveChoiceId(player);
				if (IsLocalPlayer(runManager, player))
				{
					if (TrySyncLocalHextechChoice(synchronizer, player, choiceId, HextechChoiceCodec.CreateActSelectionApplied(actIndex), $"act-selection-applied act={actIndex}", out uint sentChoiceId))
					{
						Log.Info($"[{ModInfo.Id}][Mayhem] ActSelectionApplied sync local: act={actIndex} player={player.NetId} choiceId={sentChoiceId}");
					}
					else
					{
						Log.Warn($"[{ModInfo.Id}][Mayhem] ActSelectionApplied sync local failed: act={actIndex} player={player.NetId} choiceId={choiceId}");
					}
					continue;
				}

			if (HextechAiTeammateCompat.ShouldAutoSelectRune(player))
			{
				Log.Info($"[{ModInfo.Id}][Mayhem] ActSelectionApplied AI auto-ack: act={actIndex} player={player.NetId} choiceId={choiceId}");
				continue;
			}

			pendingAcks.Add(WaitForRemoteActSelectionApplied(synchronizer, runState, player, choiceId, actIndex));
		}

		if (pendingAcks.Count == 0)
		{
			return;
		}

		Log.Info($"[{ModInfo.Id}][Mayhem] ActSelectionApplied waiting: act={actIndex} remoteCount={pendingAcks.Count}");
		Task allAcks = Task.WhenAll(pendingAcks);
		Task timeout = WaitForFramesOrRunChangeAsync(runState, ActSelectionAppliedAckTimeoutFrames);
		if (await Task.WhenAny(allAcks, timeout) == allAcks)
		{
			await allAcks;
			Log.Info($"[{ModInfo.Id}][Mayhem] ActSelectionApplied complete: act={actIndex}");
			return;
		}

		int completed = pendingAcks.Count(static task => task.IsCompletedSuccessfully);
		Log.Warn($"[{ModInfo.Id}][Mayhem] ActSelectionApplied timeout: act={actIndex} completed={completed}/{pendingAcks.Count}; continuing to avoid blocking map flow");
	}

	private static async Task WaitForRemoteActSelectionApplied(PlayerChoiceSynchronizer synchronizer, RunState runState, Player player, uint choiceId, int actIndex)
	{
		try
		{
			(PlayerChoiceResult remoteAck, uint receivedChoiceId) = await WaitForRemoteHextechChoice(
				synchronizer,
				runState,
				player,
				choiceId,
				result => HextechChoiceCodec.TryDecodeActSelectionApplied(result, actIndex),
				$"act-selection-applied act={actIndex}");
			if (!HextechChoiceCodec.TryDecodeActSelectionApplied(remoteAck, actIndex))
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem] ActSelectionApplied malformed ack: act={actIndex} player={player.NetId} choiceId={choiceId}");
				return;
			}

			Log.Info($"[{ModInfo.Id}][Mayhem] ActSelectionApplied remote: act={actIndex} player={player.NetId} choiceId={receivedChoiceId}");
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] ActSelectionApplied wait failed: act={actIndex} player={player.NetId} choiceId={choiceId} error={ex}");
		}
	}

	private static async Task WaitForFramesOrRunChangeAsync(RunState runState, int frameCount)
	{
		TimeSpan timeout = GetNetworkChoiceTimeoutDuration(frameCount);
		if (timeout <= TimeSpan.Zero)
		{
			return;
		}

		DateTimeOffset deadline = DateTimeOffset.UtcNow + timeout;
		while (IsCurrentRun(runState) && DateTimeOffset.UtcNow < deadline)
		{
			// Multiplayer timer mods can accelerate process frames; keep network choice
			// fallbacks on wall time so clients do not resolve different selection state.
			if (NGame.Instance?.IsInsideTree() == true)
			{
				await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			else
			{
				TimeSpan remaining = deadline - DateTimeOffset.UtcNow;
				if (remaining <= TimeSpan.Zero)
				{
					return;
				}

				await Task.Delay(remaining < TimeSpan.FromMilliseconds(16) ? remaining : TimeSpan.FromMilliseconds(16));
			}
		}
	}

	internal static TimeSpan GetNetworkChoiceTimeoutDuration(int frameCount)
	{
		return frameCount <= 0
			? TimeSpan.Zero
			: TimeSpan.FromSeconds(frameCount / 60.0d);
	}

	internal static async Task<PlayerChoiceSynchronizer?> WaitForPlayerChoiceSynchronizerAsync(RunManager runManager)
	{
		for (int i = 0; i < 60; i++)
		{
			if (runManager.PlayerChoiceSynchronizer != null)
			{
				return runManager.PlayerChoiceSynchronizer;
			}

			await Task.Yield();
		}

		return runManager.PlayerChoiceSynchronizer;
	}

	internal static bool IsLocalPlayer(RunManager runManager, Player player)
	{
		return player.NetId != 0UL && player.NetId == runManager.NetService.NetId;
	}
}
