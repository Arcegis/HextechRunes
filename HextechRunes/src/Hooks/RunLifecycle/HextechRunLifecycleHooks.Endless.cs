using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static partial class HextechRunLifecycleHooks
{
	internal static void HandleEndlessLoopReset(HextechMayhemModifier modifier, string reason)
	{
		SubscribeRoomEnteredIfNeeded(force: true);
		SubscribeRoomExitedIfNeeded(force: true);
		HextechRuneSelectionCoordinator.ResetActSelectionState();
		TaskHelper.RunSafely(HandleEndlessLoopActSelection(modifier, reason));
	}

	private static async Task HandleEndlessLoopActSelection(HextechMayhemModifier modifier, string reason)
	{
		RunState runState = modifier.ActiveRunState;
		for (int frame = 0; frame < 600 && IsCurrentRun(runState); frame++)
		{
			int actIndex = runState.CurrentActIndex;
			if (actIndex == 0)
			{
				if (modifier.IsActResolved(actIndex))
				{
					Log.Info($"[{ModInfo.Id}][Mayhem] Endless loop selection skipped: act0 already resolved reason={reason} frame={frame}");
					return;
				}

				if (ShouldDeferActSelectionUntilAfterCurrentEvent(runState))
				{
					Log.Info($"[{ModInfo.Id}][Mayhem] Endless loop selection deferred to ancient event proceed reason={reason} frame={frame} {DescribeCurrentEventState(runState)}");
					return;
				}

				if (runState.CurrentRoom != null || NMapScreen.Instance?.IsOpen == true)
				{
					Log.Info($"[{ModInfo.Id}][Mayhem] Endless loop selection starting: reason={reason} frame={frame} room={runState.CurrentRoom?.GetType().Name ?? "null"} mapOpen={NMapScreen.Instance?.IsOpen == true}");
					await HextechRuneSelectionCoordinator.HandleActSelection(runState, modifier);
					return;
				}
			}

			if (frame % 120 == 0)
			{
				Log.Info($"[{ModInfo.Id}][Mayhem] Endless loop selection waiting: reason={reason} frame={frame} act={actIndex} room={runState.CurrentRoom?.GetType().Name ?? "null"} mapOpen={NMapScreen.Instance?.IsOpen == true}");
			}

			await WaitOneFrame();
		}

		Log.Warn($"[{ModInfo.Id}][Mayhem] Endless loop selection timed out: reason={reason} currentRun={IsCurrentRun(runState)} act={runState.CurrentActIndex} room={runState.CurrentRoom?.GetType().Name ?? "null"} mapOpen={NMapScreen.Instance?.IsOpen == true}");
	}
}
