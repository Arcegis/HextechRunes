using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Nodes;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	private static async Task SynchronizeActSelectionApplied(RunState runState, PlayerChoiceSynchronizer synchronizer, int actIndex, int choiceOrdinal)
	{
		RunManager runManager = RunManager.Instance;
		List<Task> pendingAcks = [];
		foreach (Player player in runState.Players)
		{
			uint choiceId = synchronizer.ReserveChoiceId(player);
			if (IsLocalPlayer(runManager, player))
			{
				if (TrySyncLocalHextechChoice(
					synchronizer,
					player,
					choiceId,
					HextechChoiceCodec.CreateActSelectionApplied(actIndex, choiceOrdinal),
					$"act-selection-applied act={actIndex} ordinal={choiceOrdinal}",
					out uint sentChoiceId))
				{
					HextechLog.Info($"[{ModInfo.Id}][Mayhem] ActSelectionApplied sync local: act={actIndex} ordinal={choiceOrdinal} player={player.NetId} choiceId={sentChoiceId}");
				}
				else
				{
					Log.Warn($"[{ModInfo.Id}][Mayhem] ActSelectionApplied sync local failed: act={actIndex} ordinal={choiceOrdinal} player={player.NetId} choiceId={choiceId}");
				}
				continue;
			}

			pendingAcks.Add(WaitForRemoteActSelectionApplied(synchronizer, runState, player, choiceId, actIndex, choiceOrdinal));
		}

		if (pendingAcks.Count == 0)
		{
			return;
		}

		HextechLog.Info($"[{ModInfo.Id}][Mayhem] ActSelectionApplied waiting: act={actIndex} ordinal={choiceOrdinal} remoteCount={pendingAcks.Count}");
		Task allAcks = Task.WhenAll(pendingAcks);
		using CancellationTokenSource interruptedWaitCancellation = new();
		Task interrupted = WaitForRunChangeOrMultiplayerDisconnectAsync(runState, interruptedWaitCancellation.Token);
		if (await Task.WhenAny(allAcks, interrupted) == allAcks)
		{
			interruptedWaitCancellation.Cancel();
			await interrupted;
			await allAcks;
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] ActSelectionApplied complete: act={actIndex} ordinal={choiceOrdinal}");
			return;
		}

		int completed = pendingAcks.Count(static task => task.IsCompletedSuccessfully);
		Log.Warn($"[{ModInfo.Id}][Mayhem] ActSelectionApplied interrupted: act={actIndex} ordinal={choiceOrdinal} completed={completed}/{pendingAcks.Count} runActive={IsCurrentRun(runState)} connected={IsMultiplayerConnected()}; continuing because run changed or multiplayer disconnected");
	}

	private static async Task WaitForRemoteActSelectionApplied(PlayerChoiceSynchronizer synchronizer, RunState runState, Player player, uint choiceId, int actIndex, int choiceOrdinal)
	{
		try
		{
			(PlayerChoiceResult remoteAck, uint receivedChoiceId) = await WaitForRemoteHextechChoice(
				synchronizer,
				runState,
				player,
				choiceId,
				result => HextechChoiceCodec.TryDecodeActSelectionApplied(result, actIndex, choiceOrdinal),
				$"act-selection-applied act={actIndex} ordinal={choiceOrdinal}");
			if (!HextechChoiceCodec.TryDecodeActSelectionApplied(remoteAck, actIndex, choiceOrdinal))
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem] ActSelectionApplied malformed ack: act={actIndex} ordinal={choiceOrdinal} player={player.NetId} choiceId={choiceId}");
				return;
			}

			HextechLog.Info($"[{ModInfo.Id}][Mayhem] ActSelectionApplied remote: act={actIndex} ordinal={choiceOrdinal} player={player.NetId} choiceId={receivedChoiceId}");
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] ActSelectionApplied wait failed: act={actIndex} ordinal={choiceOrdinal} player={player.NetId} choiceId={choiceId} error={ex}");
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

	private static async Task WaitForRunChangeOrMultiplayerDisconnectAsync(RunState runState, CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested && IsCurrentRun(runState) && IsMultiplayerConnected())
		{
			if (NGame.Instance?.IsInsideTree() == true)
			{
				await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			else
			{
				try
				{
					await Task.Delay(TimeSpan.FromMilliseconds(16), cancellationToken);
				}
				catch (OperationCanceledException)
				{
					return;
				}
			}
		}
	}

	private static bool IsMultiplayerConnected()
	{
		INetGameService netService = RunManager.Instance.NetService;
		return netService.Type is NetGameType.Host or NetGameType.Client && netService.IsConnected;
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
		return LocalContext.IsMe(player)
			|| (player.NetId != 0UL && player.NetId == runManager.NetService.NetId);
	}
}
