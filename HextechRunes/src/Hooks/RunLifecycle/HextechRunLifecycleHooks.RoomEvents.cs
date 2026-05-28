using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;

namespace HextechRunes;

internal static partial class HextechRunLifecycleHooks
{
	private static void SubscribeRoomEnteredIfNeeded(bool force = false)
	{
		RunManager manager = RunManager.Instance;
		if (_subscribedRoomEntered && ReferenceEquals(_subscribedRoomEnteredManager, manager))
		{
			if (!force)
			{
				return;
			}

			manager.RoomEntered -= OnRoomEntered;
		}
		else if (_subscribedRoomEnteredManager != null)
		{
			_subscribedRoomEnteredManager.RoomEntered -= OnRoomEntered;
		}

		manager.RoomEntered += OnRoomEntered;
		_subscribedRoomEntered = true;
		_subscribedRoomEnteredManager = manager;
	}

	private static void SubscribeRoomExitedIfNeeded(bool force = false)
	{
		RunManager manager = RunManager.Instance;
		if (_subscribedRoomExited && ReferenceEquals(_subscribedRoomExitedManager, manager))
		{
			if (!force)
			{
				return;
			}

			manager.RoomExited -= OnRoomExited;
		}
		else if (_subscribedRoomExitedManager != null)
		{
			_subscribedRoomExitedManager.RoomExited -= OnRoomExited;
		}

		manager.RoomExited += OnRoomExited;
		_subscribedRoomExited = true;
		_subscribedRoomExitedManager = manager;
	}

	private static void OnRoomEntered()
	{
		if (RunManager.Instance.DebugOnlyGetState() is not RunState runState)
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] OnRoomEntered: no run state");
			return;
		}

		HextechMayhemModifier? modifier = GetMayhemModifier(runState);
		if (modifier == null && !RunsInsideStartRunOrig.Contains(runState))
		{
			modifier = GetOrRecoverMayhemModifier(runState, $"OnRoomEntered recovered missing modifier room={runState.CurrentRoom?.GetType().Name ?? "null"} actIndex={runState.CurrentActIndex}");
		}

		if (modifier != null && !modifier.IsActResolved(runState.CurrentActIndex) && modifier.TryRecoverResolvedActsFromPlayerRelics(nameof(OnRoomEntered)))
		{
			HextechEnemyUi.Refresh(modifier);
		}

		Log.Info($"[{ModInfo.Id}][Mayhem] OnRoomEntered: room={runState.CurrentRoom?.GetType().Name ?? "null"} actIndex={runState.CurrentActIndex} actResolved={modifier?.IsActResolved(runState.CurrentActIndex)} startedWithNeow={runState.ExtraFields.StartedWithNeow} {DescribeCurrentEventState(runState)}");
		if (runState.CurrentRoom is EventRoom { CanonicalEvent: AncientEventModel ancientEvent }
			&& modifier != null
			&& runState.CurrentActIndex is >= 0 and <= 2
			&& !modifier.IsActResolved(runState.CurrentActIndex))
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] OnRoomEntered: pending act selection is deferred until ancient event proceed. act={runState.CurrentActIndex} event={ancientEvent.Id.Entry} {DescribeCurrentEventState(runState)}");
		}
		if (modifier != null && ShouldScheduleActSelectionOnRoomEntered(runState, modifier))
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] OnRoomEntered: scheduling selection for room={runState.CurrentRoom?.GetType().Name ?? "null"}");
			TaskHelper.RunSafely(HextechRuneSelectionCoordinator.HandleActSelection(runState, modifier));
		}

		HextechEnemyUi.HideMayhemModifierBadge();
		if (modifier != null)
		{
			HextechEnemyUi.Refresh(modifier);
		}
	}

	private static void OnRoomExited()
	{
		try
		{
			if (RunManager.Instance.DebugOnlyGetState() is not RunState runState)
			{
				Log.Info($"[{ModInfo.Id}][Mayhem] OnRoomExited: no run state");
				return;
			}

			MapPointHistoryEntry? currentHistory = runState.CurrentMapPointHistoryEntry;
			IReadOnlyList<MapPointRoomHistoryEntry>? rooms = currentHistory?.Rooms;
			MapPointRoomHistoryEntry? roomHistory = rooms != null && rooms.Count > 0 ? rooms[^1] : null;
			string modelEntry = roomHistory?.ModelId?.Entry ?? "null";
			Log.Info($"[{ModInfo.Id}][Mayhem] OnRoomExited: currentRoom={(runState.CurrentRoom?.GetType().Name ?? "null")} lastHistoryRoom={roomHistory?.RoomType} model={modelEntry}");
		}
		catch (Exception ex)
		{
			Log.Error($"[{ModInfo.Id}][Mayhem] OnRoomExited failed: {ex}");
		}
	}

	private static bool ShouldScheduleActSelectionOnRoomEntered(RunState runState, HextechMayhemModifier modifier)
	{
		int actIndex = runState.CurrentActIndex;
		if (actIndex < 0 || actIndex > 2 || modifier.IsActResolved(actIndex) || ShouldDeferActSelectionUntilAfterCurrentEvent(runState))
		{
			return false;
		}

		return runState.CurrentRoom is MapRoom || runState.CurrentRoom is not null and not EventRoom;
	}
}
