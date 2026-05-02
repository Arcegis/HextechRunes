using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechRunLifecycleHooks
{
	private static bool _subscribedRoomEntered;
	private static bool _subscribedRoomExited;
	private static HashSet<RunState>? _runsInsideStartRunOrig;

	private static HashSet<RunState> RunsInsideStartRunOrig => _runsInsideStartRunOrig ??= new HashSet<RunState>();

	private readonly record struct EventRoomProceedState(bool ShouldSelectAfterProceed, RunState RunState, string EventId);

	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(RunManager), nameof(RunManager.FinalizeStartingRelics), BindingFlags.Instance | BindingFlags.Public),
			postfix: new HarmonyMethod(typeof(HextechRunLifecycleHooks), nameof(FinalizeStartingRelicsPostfix)));
		harmony.Patch(
			RequireMethod(typeof(NGame), "StartRun", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, typeof(RunState)),
			prefix: new HarmonyMethod(typeof(HextechRunLifecycleHooks), nameof(StartRunPrefix)),
			postfix: new HarmonyMethod(typeof(HextechRunLifecycleHooks), nameof(StartRunPostfix)));
		harmony.Patch(
			RequireMethod(typeof(NEventRoom), nameof(NEventRoom.Proceed), BindingFlags.Public | BindingFlags.Static),
			prefix: new HarmonyMethod(typeof(HextechRunLifecycleHooks), nameof(EventRoomProceedPrefix)),
			postfix: new HarmonyMethod(typeof(HextechRunLifecycleHooks), nameof(EventRoomProceedPostfix)));
		harmony.Patch(
			RequireMethod(typeof(RunManager), nameof(RunManager.OnEnded), BindingFlags.Instance | BindingFlags.Public, typeof(bool)),
			prefix: new HarmonyMethod(typeof(HextechRunLifecycleHooks), nameof(RunEndedPrefix)),
			postfix: new HarmonyMethod(typeof(HextechRunLifecycleHooks), nameof(RunEndedPostfix)));
	}

	private static void FinalizeStartingRelicsPostfix(RunManager __instance, ref Task __result)
	{
		__result = FinalizeStartingRelicsAfterOriginal(__result, __instance);
	}

	private static async Task FinalizeStartingRelicsAfterOriginal(Task original, RunManager self)
	{
		await original;

		RunState? runState = self.DebugOnlyGetState();
		if (runState == null)
		{
			return;
		}

		foreach (Player player in runState.Players)
		{
			HextechRuneSelectionCoordinator.RemoveRunesFromGrabBags(player);
		}
	}

	private static void StartRunPrefix(RunState runState)
	{
		HextechGoldrendSync.ResetCombat();
		HextechRuneSelectionCoordinator.ResetActSelectionState();
		HextechEnemyUi.Clear();
		HextechEnemyUi.HideMayhemModifierBadge();
		SubscribeRoomEnteredIfNeeded();
		SubscribeRoomExitedIfNeeded();
		Log.Info($"[{ModInfo.Id}][Mayhem] StartRunDetour begin: seed={runState.Rng.StringSeed} actIndex={runState.CurrentActIndex} startedWithNeow={runState.ExtraFields.StartedWithNeow}");
		RunsInsideStartRunOrig.Add(runState);
	}

	private static void StartRunPostfix(RunState runState, ref Task __result)
	{
		__result = StartRunAfterOriginal(__result, runState);
	}

	private static async Task StartRunAfterOriginal(Task original, RunState runState)
	{
		try
		{
			await original;
		}
		finally
		{
			RunsInsideStartRunOrig.Remove(runState);
		}

		HextechMayhemModifier modifier = EnsureMayhemModifier(runState);
		Log.Info($"[{ModInfo.Id}][Mayhem] StartRunDetour end: currentRoom={runState.CurrentRoom?.GetType().Name ?? "null"} actIndex={runState.CurrentActIndex} {DescribeCurrentEventState(runState)}");
		HextechEnemyUi.HideMayhemModifierBadge();
		HextechEnemyUi.Refresh(modifier);
		if (!modifier.IsActResolved(runState.CurrentActIndex)
			&& IsCurrentRun(runState))
		{
			if (ShouldDeferAct0SelectionUntilAfterNeow(runState))
			{
				Log.Info($"[{ModInfo.Id}][Mayhem] StartRunDetour: deferring act0 selection until safe post-Neow selection point {DescribeCurrentEventState(runState)}");
			}
			else
			{
				Log.Info($"[{ModInfo.Id}][Mayhem] StartRunDetour: selecting act{runState.CurrentActIndex} hex immediately after StartRun");
				await HextechRuneSelectionCoordinator.HandleActSelection(runState, modifier);
			}
		}
	}

	private static void EventRoomProceedPrefix(out EventRoomProceedState __state)
	{
		bool shouldSelectAfterProceed = TryGetPendingAncientProceedSelection(out RunState runState, out string eventId);
		__state = new EventRoomProceedState(shouldSelectAfterProceed, runState, eventId);
		if (shouldSelectAfterProceed)
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] EventRoomProceed begin: event={eventId} {DescribeCurrentEventState(runState)}");
		}
	}

	private static void EventRoomProceedPostfix(EventRoomProceedState __state, ref Task __result)
	{
		__result = EventRoomProceedAfterOriginal(__result, __state);
	}

	private static async Task EventRoomProceedAfterOriginal(Task original, EventRoomProceedState state)
	{
		await original;

		if (!state.ShouldSelectAfterProceed)
		{
			return;
		}

		RunState runState = state.RunState;
		string eventId = state.EventId;
		if (!IsCurrentRun(runState))
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] EventRoomProceed skip: run changed after proceed event={eventId}");
			return;
		}

		HextechMayhemModifier modifier = GetOrRecoverMayhemModifier(runState, $"EventRoomProceed recovered missing modifier after proceed event={eventId}");
		if (!modifier.IsActResolved(0) && modifier.TryRecoverResolvedActsFromPlayerRelics(nameof(EventRoomProceedAfterOriginal)))
		{
			HextechEnemyUi.Refresh(modifier);
		}

		if (modifier.IsActResolved(0))
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] EventRoomProceed skip: act0 already resolved event={eventId}");
			return;
		}

		Log.Info($"[{ModInfo.Id}][Mayhem] EventRoomProceed: waiting for all ancient events before act0 selection event={eventId} mapOpen={NMapScreen.Instance?.IsOpen == true}");
		NMapScreen.Instance?.SetTravelEnabled(enabled: false);
		try
		{
			await WaitForAllCurrentEventsFinished(runState, eventId);
			if (!IsCurrentRun(runState) || modifier.IsActResolved(0))
			{
				Log.Info($"[{ModInfo.Id}][Mayhem] EventRoomProceed skip: run changed or act0 resolved after wait event={eventId}");
				return;
			}

			Log.Info($"[{ModInfo.Id}][Mayhem] EventRoomProceed: selecting act0 hex after all ancient events finished event={eventId} mapOpen={NMapScreen.Instance?.IsOpen == true}");
			await HextechRuneSelectionCoordinator.HandleActSelection(runState, modifier);
		}
		finally
		{
			if (IsCurrentRun(runState))
			{
				NMapScreen.Instance?.SetTravelEnabled(enabled: true);
			}
		}
	}

	private static void RunEndedPrefix(RunManager __instance, out RunState? __state)
	{
		__state = __instance.DebugOnlyGetState();
	}

	private static void RunEndedPostfix(RunState? __state, bool isVictory, SerializableRun __result)
	{
		HextechTelemetry.OnRunEnded(__state, __result, isVictory);
	}

	internal static HextechMayhemModifier EnsureMayhemModifier(RunState runState)
	{
		if (GetMayhemModifier(runState) is HextechMayhemModifier existing)
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] EnsureMayhemModifier: existing state preserved {existing.DescribeActState()}");
			return existing;
		}

		HextechMayhemModifier modifier = (HextechMayhemModifier)ModelDb.Modifier<HextechMayhemModifier>().ToMutable();
		modifier.ResetForNewRun();
		modifier.OnRunLoaded(runState);
		runState.AddModifierDebug(modifier);
		Log.Info($"[{ModInfo.Id}][Mayhem] EnsureMayhemModifier: added");
		return modifier;
	}

	internal static Task HandleHextechActStarted(HextechMayhemModifier modifier)
	{
		return HextechRuneSelectionCoordinator.HandleActStarted(modifier);
	}

	private static HextechMayhemModifier? GetMayhemModifier(RunState runState)
	{
		return runState.Modifiers.OfType<HextechMayhemModifier>().LastOrDefault();
	}

	private static HextechMayhemModifier GetOrRecoverMayhemModifier(RunState runState, string reason)
	{
		if (GetMayhemModifier(runState) is HextechMayhemModifier existing)
		{
			return existing;
		}

		Log.Warn($"[{ModInfo.Id}][Mayhem] {reason}; reattaching");
		return EnsureMayhemModifier(runState);
	}

	private static void SubscribeRoomEnteredIfNeeded()
	{
		if (_subscribedRoomEntered)
		{
			return;
		}

		RunManager.Instance.RoomEntered += OnRoomEntered;
		_subscribedRoomEntered = true;
	}

	private static void SubscribeRoomExitedIfNeeded()
	{
		if (_subscribedRoomExited)
		{
			return;
		}

		RunManager.Instance.RoomExited += OnRoomExited;
		_subscribedRoomExited = true;
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
			&& runState.CurrentActIndex == 0
			&& runState.ExtraFields.StartedWithNeow
			&& !modifier.IsActResolved(0))
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] OnRoomEntered: ancient start event detected. event={ancientEvent.Id.Entry} actResolved={modifier.IsActResolved(0)} {DescribeCurrentEventState(runState)}");
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

	private static bool IsCurrentRun(RunState runState)
	{
		return ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), runState);
	}

	private static bool ShouldDeferAct0SelectionUntilAfterNeow(RunState runState)
	{
		return runState.CurrentActIndex == 0
			&& runState.ExtraFields.StartedWithNeow
			&& runState.CurrentRoom is EventRoom { CanonicalEvent: AncientEventModel };
	}

	private static bool ShouldScheduleActSelectionOnRoomEntered(RunState runState, HextechMayhemModifier modifier)
	{
		int actIndex = runState.CurrentActIndex;
		if (actIndex < 0 || actIndex > 2 || modifier.IsActResolved(actIndex) || ShouldDeferAct0SelectionUntilAfterNeow(runState))
		{
			return false;
		}

		return runState.CurrentRoom is MapRoom || runState.CurrentRoom is not null and not EventRoom;
	}

	private static string DescribeCurrentEventState(RunState runState)
	{
		if (runState.CurrentRoom is not EventRoom eventRoom)
		{
			return "eventState=none";
		}

		try
		{
			EventModel localEvent = eventRoom.LocalMutableEvent;
			return $"eventState={localEvent.Id.Entry} finished={localEvent.IsFinished} options={localEvent.CurrentOptions.Count}";
		}
		catch (Exception ex)
		{
			return $"eventState={eventRoom.CanonicalEvent.Id.Entry} localUnavailable={ex.GetType().Name}";
		}
	}

	private static bool TryGetPendingAncientProceedSelection(out RunState runState, out string eventId)
	{
		runState = null!;
		eventId = "null";

		if (RunManager.Instance.DebugOnlyGetState() is not RunState currentRunState
			|| currentRunState.CurrentActIndex != 0
			|| !currentRunState.ExtraFields.StartedWithNeow
			|| currentRunState.CurrentRoom is not EventRoom { CanonicalEvent: AncientEventModel ancientEvent })
		{
			return false;
		}

		if (GetMayhemModifier(currentRunState)?.IsActResolved(0) == true)
		{
			return false;
		}

		runState = currentRunState;
		eventId = ancientEvent.Id.Entry;
		return true;
	}

	private static async Task WaitForAllCurrentEventsFinished(RunState runState, string eventId)
	{
		for (int frame = 0; IsCurrentRun(runState); frame++)
		{
			IReadOnlyList<EventModel> events = RunManager.Instance.EventSynchronizer.Events;
			int finishedCount = events.Count(static eventModel => eventModel.IsFinished);
			if (events.Count >= runState.Players.Count && finishedCount == events.Count)
			{
				Log.Info($"[{ModInfo.Id}][Mayhem] EventRoomProceed: all events finished event={eventId} count={events.Count} waitedFrames={frame}");
				return;
			}

			if (frame % 300 == 0)
			{
				Log.Info($"[{ModInfo.Id}][Mayhem] EventRoomProceed: waiting for remote events event={eventId} finished={finishedCount}/{events.Count} players={runState.Players.Count}");
			}

			await WaitOneFrame();
		}
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
}
