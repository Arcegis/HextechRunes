using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace HextechRunes;

internal static partial class HextechRunLifecycleHooks
{
	// 按帧轮询的预算与日志间隔（帧数随帧率变化，只作为"足够久"的上限，不承担时序语义）。
	private const int EnemyUiRefreshFrameBudget = 45;
	private const int ResumeAfterLoadFrameBudget = 300;
	private const int EndlessLoopActTransitionTimeoutFrames = 3600;
	private const int EndlessLoopRoomReadyTimeoutFrames = 600;
	private const int EndlessLoopWaitLogIntervalFrames = 120;
	private const int RemoteEventsWaitLogIntervalFrames = 300;
	// 等待其他玩家完成远古事件的上限：远超正常阅读/选择时间，只防止对端异常时永远挂起。
	private const int RemoteEventsWaitTimeoutFrames = 18000;

	private static readonly HashSet<RunState> RunsInsideStartRunOrig = [];

	private readonly record struct EventRoomProceedState(RunState RunState, int ActIndex, string EventId);

	/// <summary>跑局开始与读档共用：清空日志预算、战斗与敌方海克斯的跑局临时状态，并按新跑局重置夺金同步。</summary>
	private static void ResetRunScopedState(RunState runState)
	{
		HextechRunLogBudget.Reset();
		ResetTransientRunState();
		HextechGoldrendSync.ResetForRun(runState);
	}

	/// <summary>跑局开始、读档与结束都要清的战斗/敌方海克斯临时状态。</summary>
	private static void ResetTransientRunState()
	{
		HextechCombatHooks.ResetTransientCombatState();
		HextechEnemyHexEffects.ResetAllRunScopedState();
	}

	internal static HextechMayhemModifier EnsureMayhemModifier(RunState runState)
	{
		if (HextechMayhemModifier.FindIn(runState) is HextechMayhemModifier existing)
		{
			HextechLog.Info("Mayhem", $"EnsureMayhemModifier: existing state preserved {existing.DescribeActState()}");
			return existing;
		}

		HextechMayhemModifier modifier = (HextechMayhemModifier)ModelDb.Modifier<HextechMayhemModifier>().ToMutable();
		modifier.ResetForNewRun();
		modifier.OnRunLoaded(runState);
		runState.AddModifierDebug(modifier);
		HextechLog.Info("Mayhem", $"EnsureMayhemModifier: added");
		return modifier;
	}

	private static HextechMayhemModifier GetOrRecoverMayhemModifier(RunState runState, string reason)
	{
		if (HextechMayhemModifier.FindIn(runState) is HextechMayhemModifier existing)
		{
			return existing;
		}

		HextechLog.Warn("Mayhem", $"{reason}; reattaching");
		return EnsureMayhemModifier(runState);
	}

	private static bool IsCurrentRun(RunState runState)
	{
		return ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), runState);
	}

	private static bool ShouldDeferActSelectionUntilAfterCurrentEvent(RunState runState)
	{
		return runState.CurrentActIndex >= 0
			&& runState.CurrentRoom is EventRoom { CanonicalEvent: AncientEventModel };
	}

	private static int ResolveCurrentStageIndex(
		RunState runState,
		HextechMayhemModifier modifier,
		out string? extraStageId)
	{
		extraStageId = HextechRunesInterop.GetCurrentExtraActId(runState);
		if (!string.IsNullOrWhiteSpace(extraStageId))
		{
			return modifier.ActivateExtraStage(extraStageId);
		}

		modifier.ClearActiveExtraStage();
		return modifier.GetCurrentActSelectionIndex();
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
