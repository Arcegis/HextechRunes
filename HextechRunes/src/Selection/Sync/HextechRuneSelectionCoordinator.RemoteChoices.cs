using System.Collections;
using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Nodes;
using static HextechRunes.HextechHookReflection;
using static HextechRunes.HextechSelectionHelpers;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	private const BindingFlags BufferedChoiceFieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
	private static readonly FieldInfo? ReceivedChoicesField = TryGetField(
		typeof(PlayerChoiceSynchronizer),
		"_receivedChoices",
		BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly Type? ReceivedChoiceType = ReceivedChoicesField?.FieldType.GetGenericArguments().FirstOrDefault();
	private static readonly FieldInfo? ReceivedChoiceSenderIdField = TryGetReceivedChoiceField("senderId");
	private static readonly FieldInfo? ReceivedChoiceChoiceIdField = TryGetReceivedChoiceField("choiceId");
	private static readonly FieldInfo? ReceivedChoiceCompletionSourceField = TryGetReceivedChoiceField("completionSource");
	private static readonly PropertyInfo? ReceivedChoiceTaskProperty = ReceivedChoiceCompletionSourceField?.FieldType.GetProperty(
		"Task",
		BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly bool BufferedChoiceReflectionAvailable = ValidateBufferedChoiceReflection();

	internal static async Task<(PlayerChoiceResult Result, uint ChoiceId)> WaitForRemoteHextechChoice(
		PlayerChoiceSynchronizer synchronizer,
		RunState runState,
		Player player,
		uint initialChoiceId,
		Func<PlayerChoiceResult, bool> isExpected,
		string context,
		Func<PlayerChoiceResult, bool>? shouldReturnMalformedExactChoice = null,
		CancellationToken cancellationToken = default)
	{
		(PlayerChoiceResult Result, uint ChoiceId)? result = await TryWaitForRemoteHextechChoice(
			synchronizer,
			runState,
			player,
			initialChoiceId,
			isExpected,
			context,
			timeoutFrames: null,
			shouldReturnMalformedExactChoice: shouldReturnMalformedExactChoice,
			cancellationToken: cancellationToken);
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
		Func<bool>? shouldContinueAfterTimeout = null,
		Func<PlayerChoiceResult, bool>? shouldReturnMalformedExactChoice = null,
		CancellationToken cancellationToken = default)
	{
		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();
			(PlayerChoiceResult Result, uint ChoiceId)? remote = await WaitForRemoteChoiceByEvent(
				synchronizer,
				runState,
				player,
				initialChoiceId,
				context,
				timeoutFrames,
				shouldContinueAfterTimeout,
				cancellationToken);
			if (!remote.HasValue)
			{
				if (!IsCurrentRun(runState)
					|| !IsMultiplayerConnected()
					|| shouldContinueAfterTimeout?.Invoke() == false)
				{
					throw new OperationCanceledException(
						$"Remote choice wait was canceled: context={context} player={player.NetId} choiceId={initialChoiceId}.");
				}

				if (shouldContinueAfterTimeout?.Invoke() == true)
				{
					HextechLog.Warn("Mayhem", $"WaitForRemoteHextechChoice: still waiting context={context} player={player.NetId} choiceId={initialChoiceId}");
					continue;
				}

				HextechLog.Warn("Mayhem", $"WaitForRemoteHextechChoice: interrupted context={context} player={player.NetId} choiceId={initialChoiceId}");
				return null;
			}

			PlayerChoiceResult remoteChoice = remote.Value.Result;
			uint receivedChoiceId = remote.Value.ChoiceId;
			if (isExpected(remoteChoice)
				|| (receivedChoiceId == initialChoiceId
					&& shouldReturnMalformedExactChoice?.Invoke(remoteChoice) == true))
			{
				return (remoteChoice, receivedChoiceId);
			}

			throw CreateProtocolFailure(
				context,
				$"Unexpected choice payload context={context} player={player.NetId} " +
				$"choiceId={initialChoiceId} type={remoteChoice.ChoiceType} result={remoteChoice}");
		}
	}

	internal static uint SyncLocalHextechChoice(
		PlayerChoiceSynchronizer synchronizer,
		Player player,
		uint choiceId,
		PlayerChoiceResult result,
		string context)
	{
		try
		{
			synchronizer.SyncLocalChoice(player, choiceId, result);
			return choiceId;
		}
		catch (Exception ex)
		{
			throw CreateProtocolFailure(
				context,
				$"Failed to send local choice context={context} player={player.NetId} choiceId={choiceId}",
				ex);
		}
	}

	private static async Task<(PlayerChoiceResult Result, uint ChoiceId)?> WaitForRemoteChoiceByEvent(
		PlayerChoiceSynchronizer synchronizer,
		RunState runState,
		Player player,
		uint choiceId,
		string context,
		int? timeoutFrames,
		Func<bool>? shouldRemainActive,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		ThrowIfSelectionTransactionInactive(runState, shouldRemainActive, context);

		if (TryTakeBufferedRemoteChoice(synchronizer, player, choiceId, out NetPlayerChoiceResult bufferedResult))
		{
			HextechLog.Info("Mayhem", $"RemoteChoice event wait: consumed buffered choice context={context} player={player.NetId} choiceId={choiceId}");
			return (PlayerChoiceResult.FromNetData(player, runState, bufferedResult), choiceId);
		}

		TaskCompletionSource<(uint ChoiceId, NetPlayerChoiceResult Result)> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		void OnPlayerChoiceReceived(Player receivedPlayer, uint receivedChoiceId, NetPlayerChoiceResult result)
		{
			if (receivedPlayer.NetId != player.NetId)
			{
				return;
			}

			if (receivedChoiceId == choiceId)
			{
				completion.TrySetResult((receivedChoiceId, result));
			}
		}

		synchronizer.PlayerChoiceReceived += OnPlayerChoiceReceived;
		try
		{
			ThrowIfSelectionTransactionInactive(runState, shouldRemainActive, context);
			if (TryTakeBufferedRemoteChoice(synchronizer, player, choiceId, out NetPlayerChoiceResult lateBufferedResult))
			{
				HextechLog.Info("Mayhem", $"RemoteChoice event wait: consumed late buffered choice context={context} player={player.NetId} choiceId={choiceId}");
				return (PlayerChoiceResult.FromNetData(player, runState, lateBufferedResult), choiceId);
			}

			Task<(uint ChoiceId, NetPlayerChoiceResult Result)> waitTask = completion.Task;
			using CancellationTokenSource observerCancellation =
				CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			Task interrupted = WaitForSelectionTransactionInterruptionAsync(
				runState,
				shouldRemainActive,
				observerCancellation.Token);
			Task? timeout = timeoutFrames.HasValue
				? WaitForFramesOrRunChangeAsync(runState, timeoutFrames.Value, observerCancellation.Token)
				: null;
			Task winner = timeout == null
				? await Task.WhenAny(waitTask, interrupted)
				: await Task.WhenAny(waitTask, interrupted, timeout);
			if (winner != waitTask)
			{
				observerCancellation.Cancel();
				ObserveCompletion(interrupted, $"{context} interruption observer");
				if (timeout != null)
				{
					ObserveCompletion(timeout, $"{context} timeout observer");
				}

				if (cancellationToken.IsCancellationRequested)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}

				return null;
			}

			(uint receivedChoiceId, NetPlayerChoiceResult result) = await waitTask;
			observerCancellation.Cancel();
			ObserveCompletion(interrupted, $"{context} interruption observer");
			if (timeout != null)
			{
				ObserveCompletion(timeout, $"{context} timeout observer");
			}
			TryTakeBufferedRemoteChoice(synchronizer, player, receivedChoiceId, out _);
			HextechLog.Info("Mayhem", $"RemoteChoice event wait: received choice context={context} player={player.NetId} expectedChoiceId={choiceId} receivedChoiceId={receivedChoiceId}");
			return (PlayerChoiceResult.FromNetData(player, runState, result), receivedChoiceId);
		}
		finally
		{
			synchronizer.PlayerChoiceReceived -= OnPlayerChoiceReceived;
		}
	}

	private static void ThrowIfSelectionTransactionInactive(
		RunState runState,
		Func<bool>? shouldRemainActive,
		string context)
	{
		if (!IsCurrentRun(runState)
			|| !IsMultiplayerConnected()
			|| shouldRemainActive?.Invoke() == false)
		{
			throw new OperationCanceledException(
				$"Multiplayer choice transaction is no longer active: {context}");
		}
	}

	private static async Task WaitForSelectionTransactionInterruptionAsync(
		RunState runState,
		Func<bool>? shouldRemainActive,
		CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested
			&& IsCurrentRun(runState)
			&& IsMultiplayerConnected()
			&& shouldRemainActive?.Invoke() != false)
		{
			await WaitForProcessFrameOrDelayAsync(cancellationToken);
		}
	}

	private static void ObserveCompletion(Task task, string context)
	{
		_ = ObserveCompletionAsync(task, context);
	}

	private static async Task ObserveCompletionAsync(Task task, string context)
	{
		try
		{
			await task;
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			HextechLog.Error("Mayhem", $"Background choice observer failed: context={context} error={ex}");
		}
	}

	internal static void AbortMultiplayerChoiceTransaction(string context, string reason)
	{
		try
		{
			if (IsMultiplayerConnected())
			{
				HextechLog.Error("Mayhem", $"Aborting multiplayer choice transaction: context={context} reason={reason}");
				RunManager.Instance.NetService.Disconnect(NetError.InternalError, now: true);
			}
		}
		catch (Exception disconnectError)
		{
			HextechLog.Error("Mayhem", $"Failed to abort multiplayer choice transaction: context={context} error={disconnectError}");
		}
	}

	/// <summary>
	/// 联机选择协议失败的唯一出口:断开本次联机事务、记录一次原因(含内层异常),返回待抛出的异常。
	/// 调用方直接 <c>throw CreateProtocolFailure(...)</c>,不要再自行记录同一条错误。
	/// </summary>
	internal static HextechChoiceProtocolException CreateProtocolFailure(
		string context,
		string reason,
		Exception? innerException = null)
	{
		AbortMultiplayerChoiceTransaction(context, innerException == null ? reason : $"{reason}: {innerException}");
		return innerException == null
			? new HextechChoiceProtocolException(reason)
			: new HextechChoiceProtocolException(reason, innerException);
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
			if (!TryGetBufferedChoices(synchronizer, out IList? receivedChoices))
			{
				return false;
			}

			foreach ((int index, ulong senderId, uint bufferedChoiceId, Task<NetPlayerChoiceResult> task) in EnumerateBufferedChoices(receivedChoices))
			{
				if (senderId != player.NetId || bufferedChoiceId != choiceId)
				{
					continue;
				}

				result = task.Result;
				receivedChoices.RemoveAt(index);
				return true;
			}
		}
		catch (Exception ex)
		{
			HextechLog.Warn("Mayhem", $"RemoteChoice buffered read failed: player={player.NetId} choiceId={choiceId} error={ex}");
		}

		return false;
	}

	private static FieldInfo? TryGetReceivedChoiceField(string name)
	{
		return ReceivedChoiceType == null
			? null
			: TryGetField(ReceivedChoiceType, name, BufferedChoiceFieldFlags);
	}

	private static bool TryGetBufferedChoices(PlayerChoiceSynchronizer synchronizer, [NotNullWhen(true)] out IList? receivedChoices)
	{
		receivedChoices = ReceivedChoicesField?.GetValue(synchronizer) as IList;
		return receivedChoices != null;
	}

	private static IEnumerable<(int Index, ulong SenderId, uint ChoiceId, Task<NetPlayerChoiceResult> Task)> EnumerateBufferedChoices(IList receivedChoices)
	{
		FieldInfo? senderIdField = ReceivedChoiceSenderIdField;
		FieldInfo? choiceIdField = ReceivedChoiceChoiceIdField;
		FieldInfo? completionSourceField = ReceivedChoiceCompletionSourceField;
		PropertyInfo? taskProperty = ReceivedChoiceTaskProperty;
		if (!BufferedChoiceReflectionAvailable
			|| senderIdField == null
			|| choiceIdField == null
			|| completionSourceField == null
			|| taskProperty == null)
		{
			yield break;
		}

		for (int i = 0; i < receivedChoices.Count; i++)
		{
			object? entry = receivedChoices[i];
			if (entry == null
				|| senderIdField.GetValue(entry) is not ulong senderId
				|| choiceIdField.GetValue(entry) is not uint choiceId)
			{
				continue;
			}

			object? completionSource = completionSourceField.GetValue(entry);
			if (completionSource == null
				|| taskProperty.GetValue(completionSource) is not Task<NetPlayerChoiceResult> task
				|| !task.IsCompletedSuccessfully)
			{
				continue;
			}

			yield return (i, senderId, choiceId, task);
		}
	}

	private static bool ValidateBufferedChoiceReflection()
	{
		if (ReceivedChoiceType == null)
		{
			HextechLog.Warn("Mayhem", "RemoteChoice buffered reflection unavailable: could not resolve ReceivedChoice type; using event path.");
			return false;
		}

		if (ReceivedChoiceTaskProperty == null)
		{
			HextechLog.Warn("Mayhem", "RemoteChoice buffered reflection unavailable: could not resolve completionSource.Task; using event path.");
			return false;
		}

		return ReceivedChoiceSenderIdField != null
			&& ReceivedChoiceChoiceIdField != null
			&& ReceivedChoiceCompletionSourceField != null;
	}

}

internal sealed class HextechChoiceProtocolException : InvalidOperationException
{
	internal HextechChoiceProtocolException(string message)
		: base(message)
	{
	}

	internal HextechChoiceProtocolException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
