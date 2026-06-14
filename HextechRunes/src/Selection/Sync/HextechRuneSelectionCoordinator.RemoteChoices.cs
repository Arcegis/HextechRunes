using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	internal static async Task<(PlayerChoiceResult Result, uint ChoiceId)> WaitForRemoteHextechChoice(
		PlayerChoiceSynchronizer synchronizer,
		RunState runState,
		Player player,
		uint initialChoiceId,
		Func<PlayerChoiceResult, bool> isExpected,
		string context)
	{
		uint choiceId = initialChoiceId;
		int skipped = 0;
		while (true)
		{
			PlayerChoiceResult remoteChoice = await WaitForRemoteChoiceByEvent(synchronizer, runState, player, choiceId, context);
			if (isExpected(remoteChoice))
			{
				if (skipped > 0)
				{
					Log.Info($"[{ModInfo.Id}][Mayhem] WaitForRemoteHextechChoice: accepted after skipping foreign choices context={context} player={player.NetId} choiceId={choiceId} skipped={skipped}");
				}

				return (remoteChoice, choiceId);
			}

			skipped++;
			Log.Warn($"[{ModInfo.Id}][Mayhem] WaitForRemoteHextechChoice: skipped non-hextech choice context={context} player={player.NetId} choiceId={choiceId} skipped={skipped} type={remoteChoice.ChoiceType} result={remoteChoice}");
			choiceId = synchronizer.ReserveChoiceId(player);
		}
	}

	private static async Task<PlayerChoiceResult> WaitForRemoteChoiceByEvent(
		PlayerChoiceSynchronizer synchronizer,
		RunState runState,
		Player player,
		uint choiceId,
		string context)
	{
		if (TryTakeBufferedRemoteChoice(synchronizer, player, choiceId, out NetPlayerChoiceResult bufferedResult))
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] RemoteChoice event wait: consumed buffered choice context={context} player={player.NetId} choiceId={choiceId}");
			return PlayerChoiceResult.FromNetData(player, runState, bufferedResult);
		}

		TaskCompletionSource<NetPlayerChoiceResult> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		void OnPlayerChoiceReceived(Player receivedPlayer, uint receivedChoiceId, NetPlayerChoiceResult result)
		{
			if (receivedPlayer.NetId == player.NetId && receivedChoiceId == choiceId)
			{
				completion.TrySetResult(result);
			}
		}

		synchronizer.PlayerChoiceReceived += OnPlayerChoiceReceived;
		try
		{
			if (TryTakeBufferedRemoteChoice(synchronizer, player, choiceId, out NetPlayerChoiceResult lateBufferedResult))
			{
				Log.Info($"[{ModInfo.Id}][Mayhem] RemoteChoice event wait: consumed late buffered choice context={context} player={player.NetId} choiceId={choiceId}");
				return PlayerChoiceResult.FromNetData(player, runState, lateBufferedResult);
			}

			NetPlayerChoiceResult result = await completion.Task;
			Log.Info($"[{ModInfo.Id}][Mayhem] RemoteChoice event wait: received choice context={context} player={player.NetId} choiceId={choiceId}");
			return PlayerChoiceResult.FromNetData(player, runState, result);
		}
		finally
		{
			synchronizer.PlayerChoiceReceived -= OnPlayerChoiceReceived;
		}
	}

	private static bool TryTakeBufferedRemoteChoice(
		PlayerChoiceSynchronizer synchronizer,
		Player player,
		uint choiceId,
		out NetPlayerChoiceResult result)
	{
		result = default;
		try
		{
			FieldInfo? receivedChoicesField = typeof(PlayerChoiceSynchronizer).GetField("_receivedChoices", BindingFlags.Instance | BindingFlags.NonPublic);
			if (receivedChoicesField?.GetValue(synchronizer) is not IList receivedChoices)
			{
				return false;
			}

			for (int i = 0; i < receivedChoices.Count; i++)
			{
				object? entry = receivedChoices[i];
				if (entry == null)
				{
					continue;
				}

				Type entryType = entry.GetType();
				ulong senderId = (ulong)(entryType.GetField("senderId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(entry) ?? 0UL);
				uint bufferedChoiceId = (uint)(entryType.GetField("choiceId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(entry) ?? uint.MaxValue);
				if (senderId != player.NetId || bufferedChoiceId != choiceId)
				{
					continue;
				}

				object? completionSource = entryType.GetField("completionSource", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(entry);
				if (completionSource?.GetType().GetProperty("Task")?.GetValue(completionSource) is not Task<NetPlayerChoiceResult> task
					|| !task.IsCompletedSuccessfully)
				{
					continue;
				}

				result = task.Result;
				receivedChoices.RemoveAt(i);
				return true;
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] RemoteChoice buffered read failed: player={player.NetId} choiceId={choiceId} error={ex}");
		}

		return false;
	}
}
