using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes;
using static HextechRunes.HextechSelectionHelpers;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	private static async Task SynchronizeActSelectionApplied(
		RunState runState,
		PlayerChoiceSynchronizer synchronizer,
		int actIndex,
		int choiceOrdinal,
		CancellationToken cancellationToken)
	{
		RunManager runManager = RunManager.Instance;
		List<Task> pendingAcks = [];
		foreach (Player player in runState.Players)
		{
			cancellationToken.ThrowIfCancellationRequested();
			uint choiceId = synchronizer.ReserveChoiceId(player);
			if (IsLocalPlayer(runManager, player))
			{
				uint sentChoiceId = SyncLocalHextechChoice(
					synchronizer,
					player,
					choiceId,
					HextechChoiceCodec.CreateActSelectionApplied(actIndex, choiceOrdinal),
					$"act-selection-applied act={actIndex} ordinal={choiceOrdinal}");
				HextechLog.Info("Mayhem", $"ActSelectionApplied sync local: act={actIndex} ordinal={choiceOrdinal} player={player.NetId} choiceId={sentChoiceId}");
				continue;
			}

			pendingAcks.Add(WaitForRemoteActSelectionApplied(
				synchronizer,
				runState,
				player,
				choiceId,
				actIndex,
				choiceOrdinal,
				cancellationToken));
		}

		if (pendingAcks.Count == 0)
		{
			return;
		}

		HextechLog.Info("Mayhem", $"ActSelectionApplied waiting: act={actIndex} ordinal={choiceOrdinal} remoteCount={pendingAcks.Count}");
		await Task.WhenAll(pendingAcks);
		HextechLog.Info("Mayhem", $"ActSelectionApplied complete: act={actIndex} ordinal={choiceOrdinal}");
	}

	private static async Task WaitForRemoteActSelectionApplied(
		PlayerChoiceSynchronizer synchronizer,
		RunState runState,
		Player player,
		uint choiceId,
		int actIndex,
		int choiceOrdinal,
		CancellationToken cancellationToken)
	{
		// isExpected 已完整解码校验;等待只会在它返回 true 时结束,不再二次解码。
		(_, uint receivedChoiceId) = await WaitForRemoteHextechChoice(
			synchronizer,
			runState,
			player,
			choiceId,
			result => HextechChoiceCodec.TryDecodeActSelectionApplied(result, actIndex, choiceOrdinal),
			$"act-selection-applied act={actIndex} ordinal={choiceOrdinal}",
			cancellationToken: cancellationToken);

		HextechLog.Info("Mayhem", $"ActSelectionApplied remote: act={actIndex} ordinal={choiceOrdinal} player={player.NetId} choiceId={receivedChoiceId}");
	}

	private static async Task WaitForFramesOrRunChangeAsync(
		RunState runState,
		int frameCount,
		CancellationToken cancellationToken = default)
	{
		TimeSpan timeout = GetNetworkChoiceTimeoutDuration(frameCount);
		if (timeout <= TimeSpan.Zero)
		{
			return;
		}

		DateTimeOffset deadline = DateTimeOffset.UtcNow + timeout;
		while (!cancellationToken.IsCancellationRequested
			&& IsCurrentRun(runState)
			&& IsMultiplayerConnected()
			&& DateTimeOffset.UtcNow < deadline)
		{
			// Multiplayer timer mods can accelerate process frames; keep network choice
			// fallbacks on wall time so clients do not resolve different selection state.
			await WaitForProcessFrameOrDelayAsync(cancellationToken);
		}

		cancellationToken.ThrowIfCancellationRequested();
	}

	internal static TimeSpan GetNetworkChoiceTimeoutDuration(int frameCount)
	{
		return frameCount <= 0
			? TimeSpan.Zero
			: TimeSpan.FromSeconds(frameCount / 60.0d);
	}

	internal static Task<PlayerChoiceSynchronizer> WaitForPlayerChoiceSynchronizerAsync(RunManager runManager)
	{
		PlayerChoiceSynchronizer? synchronizer = runManager.PlayerChoiceSynchronizer;
		if (synchronizer != null)
		{
			return Task.FromResult(synchronizer);
		}

		throw CreateProtocolFailure(
			"player-choice-synchronizer",
			"PlayerChoiceSynchronizer is unavailable during an active multiplayer transaction.");
	}

	internal static bool IsLocalPlayer(RunManager runManager, Player player)
	{
		return LocalContext.IsMe(player)
			|| (player.NetId != 0UL && player.NetId == runManager.NetService.NetId);
	}
}
