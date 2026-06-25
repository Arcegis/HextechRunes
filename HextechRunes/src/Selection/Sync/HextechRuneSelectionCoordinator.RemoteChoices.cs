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
		(PlayerChoiceResult Result, uint ChoiceId)? result = await TryWaitForRemoteHextechChoice(
			synchronizer,
			runState,
			player,
			initialChoiceId,
			isExpected,
			context,
			timeoutFrames: null);
		if (result.HasValue)
		{
			return result.Value;
		}

		throw new TimeoutException($"Timed out waiting for remote hextech choice context={context} player={player.NetId} choiceId={initialChoiceId}.");
	}

	internal static async Task<(PlayerChoiceResult Result, uint ChoiceId)?> TryWaitForRemoteHextechChoice(
		PlayerChoiceSynchronizer synchronizer,
		RunState runState,
		Player player,
		uint initialChoiceId,
		Func<PlayerChoiceResult, bool> isExpected,
		string context,
		int? timeoutFrames,
		Func<bool>? shouldContinueAfterTimeout = null)
	{
		uint choiceId = initialChoiceId;
		int skipped = 0;
		while (true)
		{
			(PlayerChoiceResult Result, uint ChoiceId)? remote = await WaitForRemoteChoiceByEvent(
				synchronizer,
				runState,
				player,
				choiceId,
				isExpected,
				context,
				timeoutFrames);
			if (!remote.HasValue)
			{
				if (shouldContinueAfterTimeout?.Invoke() == true)
				{
					Log.Warn($"[{ModInfo.Id}][Mayhem] WaitForRemoteHextechChoice: still waiting context={context} player={player.NetId} choiceId={choiceId} skipped={skipped}");
					continue;
				}

				Log.Warn($"[{ModInfo.Id}][Mayhem] WaitForRemoteHextechChoice: timeout context={context} player={player.NetId} choiceId={choiceId} skipped={skipped}");
				return null;
			}

			PlayerChoiceResult remoteChoice = remote.Value.Result;
			uint receivedChoiceId = remote.Value.ChoiceId;
			if (isExpected(remoteChoice))
			{
				if (skipped > 0 || receivedChoiceId != choiceId)
				{
					HextechLog.Info($"[{ModInfo.Id}][Mayhem] WaitForRemoteHextechChoice: accepted context={context} player={player.NetId} expectedChoiceId={choiceId} receivedChoiceId={receivedChoiceId} skipped={skipped}");
				}

				return (remoteChoice, receivedChoiceId);
			}

			skipped++;
			Log.Warn($"[{ModInfo.Id}][Mayhem] WaitForRemoteHextechChoice: skipped non-hextech choice context={context} player={player.NetId} expectedChoiceId={choiceId} receivedChoiceId={receivedChoiceId} skipped={skipped} type={remoteChoice.ChoiceType} result={remoteChoice}");
			choiceId = synchronizer.ReserveChoiceId(player);
		}
	}

	internal static bool TrySyncLocalHextechChoice(
		PlayerChoiceSynchronizer synchronizer,
		Player player,
		uint choiceId,
		PlayerChoiceResult result,
		string context,
		out uint sentChoiceId)
	{
		sentChoiceId = choiceId;
		try
		{
			synchronizer.SyncLocalChoice(player, choiceId, result);
			return true;
		}
		catch (InvalidOperationException ex)
		{
			uint retryChoiceId = synchronizer.ReserveChoiceId(player);
			Log.Warn($"[{ModInfo.Id}][Mayhem] SyncLocalHextechChoice retry: context={context} player={player.NetId} staleChoiceId={choiceId} retryChoiceId={retryChoiceId} error={ex.Message}");
			try
			{
				synchronizer.SyncLocalChoice(player, retryChoiceId, result);
				sentChoiceId = retryChoiceId;
				return true;
			}
			catch (Exception retryEx)
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem] SyncLocalHextechChoice failed: context={context} player={player.NetId} choiceId={retryChoiceId} error={retryEx}");
				return false;
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] SyncLocalHextechChoice failed: context={context} player={player.NetId} choiceId={choiceId} error={ex}");
			return false;
		}
	}

	private static async Task<(PlayerChoiceResult Result, uint ChoiceId)?> WaitForRemoteChoiceByEvent(
		PlayerChoiceSynchronizer synchronizer,
		RunState runState,
		Player player,
		uint choiceId,
		Func<PlayerChoiceResult, bool> isExpected,
		string context)
		=> await WaitForRemoteChoiceByEvent(synchronizer, runState, player, choiceId, isExpected, context, timeoutFrames: null);

	private static async Task<(PlayerChoiceResult Result, uint ChoiceId)?> WaitForRemoteChoiceByEvent(
		PlayerChoiceSynchronizer synchronizer,
		RunState runState,
		Player player,
		uint choiceId,
		Func<PlayerChoiceResult, bool> isExpected,
		string context,
		int? timeoutFrames)
	{
		if (TryTakeBufferedExpectedRemoteChoice(synchronizer, runState, player, isExpected, out PlayerChoiceResult expectedBufferedResult, out uint expectedBufferedChoiceId))
		{
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] RemoteChoice event wait: consumed expected buffered choice context={context} player={player.NetId} choiceId={expectedBufferedChoiceId}");
			return (expectedBufferedResult, expectedBufferedChoiceId);
		}

		if (TryTakeBufferedRemoteChoice(synchronizer, player, choiceId, out NetPlayerChoiceResult bufferedResult))
		{
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] RemoteChoice event wait: consumed buffered choice context={context} player={player.NetId} choiceId={choiceId}");
			return (PlayerChoiceResult.FromNetData(player, runState, bufferedResult), choiceId);
		}

		TaskCompletionSource<(uint ChoiceId, NetPlayerChoiceResult Result)> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		void OnPlayerChoiceReceived(Player receivedPlayer, uint receivedChoiceId, NetPlayerChoiceResult result)
		{
			if (receivedPlayer.NetId != player.NetId)
			{
				return;
			}

			if (receivedChoiceId == choiceId || IsExpectedNetChoice(player, runState, result, isExpected))
			{
				completion.TrySetResult((receivedChoiceId, result));
			}
		}

		synchronizer.PlayerChoiceReceived += OnPlayerChoiceReceived;
		try
		{
			if (TryTakeBufferedExpectedRemoteChoice(synchronizer, runState, player, isExpected, out PlayerChoiceResult lateExpectedBufferedResult, out uint lateExpectedBufferedChoiceId))
			{
				HextechLog.Info($"[{ModInfo.Id}][Mayhem] RemoteChoice event wait: consumed late expected buffered choice context={context} player={player.NetId} choiceId={lateExpectedBufferedChoiceId}");
				return (lateExpectedBufferedResult, lateExpectedBufferedChoiceId);
			}

			if (TryTakeBufferedRemoteChoice(synchronizer, player, choiceId, out NetPlayerChoiceResult lateBufferedResult))
			{
				HextechLog.Info($"[{ModInfo.Id}][Mayhem] RemoteChoice event wait: consumed late buffered choice context={context} player={player.NetId} choiceId={choiceId}");
				return (PlayerChoiceResult.FromNetData(player, runState, lateBufferedResult), choiceId);
			}

			Task<(uint ChoiceId, NetPlayerChoiceResult Result)> waitTask = completion.Task;
			if (timeoutFrames.HasValue)
			{
				Task timeout = WaitForFramesOrRunChangeAsync(runState, timeoutFrames.Value);
				if (await Task.WhenAny(waitTask, timeout) != waitTask)
				{
					return null;
				}
			}

			(uint receivedChoiceId, NetPlayerChoiceResult result) = await waitTask;
			TryTakeBufferedRemoteChoice(synchronizer, player, receivedChoiceId, out _);
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] RemoteChoice event wait: received choice context={context} player={player.NetId} expectedChoiceId={choiceId} receivedChoiceId={receivedChoiceId}");
			return (PlayerChoiceResult.FromNetData(player, runState, result), receivedChoiceId);
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

	private static bool TryTakeBufferedExpectedRemoteChoice(
		PlayerChoiceSynchronizer synchronizer,
		RunState runState,
		Player player,
		Func<PlayerChoiceResult, bool> isExpected,
		out PlayerChoiceResult result,
		out uint choiceId)
	{
		result = null!;
		choiceId = uint.MaxValue;
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
				if (senderId != player.NetId)
				{
					continue;
				}

				object? completionSource = entryType.GetField("completionSource", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(entry);
				if (completionSource?.GetType().GetProperty("Task")?.GetValue(completionSource) is not Task<NetPlayerChoiceResult> task
					|| !task.IsCompletedSuccessfully)
				{
					continue;
				}

				PlayerChoiceResult candidate = PlayerChoiceResult.FromNetData(player, runState, task.Result);
				if (!isExpected(candidate))
				{
					continue;
				}

				choiceId = (uint)(entryType.GetField("choiceId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(entry) ?? uint.MaxValue);
				result = candidate;
				receivedChoices.RemoveAt(i);
				return true;
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] RemoteChoice expected buffered read failed: player={player.NetId} error={ex}");
		}

		return false;
	}

	private static bool IsExpectedNetChoice(
		Player player,
		RunState runState,
		NetPlayerChoiceResult netResult,
		Func<PlayerChoiceResult, bool> isExpected)
	{
		try
		{
			return isExpected(PlayerChoiceResult.FromNetData(player, runState, netResult));
		}
		catch
		{
			return false;
		}
	}
}
