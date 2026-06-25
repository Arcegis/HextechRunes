using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static partial class HextechRunLifecycleHooks
{
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
		HextechLog.Info($"[{ModInfo.Id}][Mayhem] StartRunDetour begin: seed={runState.Rng.StringSeed} actIndex={runState.CurrentActIndex} startedWithNeow={runState.ExtraFields.StartedWithNeow}");
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
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] StartRunDetour end: currentRoom={runState.CurrentRoom?.GetType().Name ?? "null"} actIndex={runState.CurrentActIndex} {DescribeCurrentEventState(runState)}");
			HextechEnemyUi.HideMayhemModifierBadge();
			HextechEnemyUi.Refresh(modifier);
			if (!modifier.IsActResolved(runState.CurrentActIndex)
				&& IsCurrentRun(runState))
		{
			if (ShouldDeferActSelectionUntilAfterCurrentEvent(runState))
			{
				HextechLog.Info($"[{ModInfo.Id}][Mayhem] StartRunDetour: deferring act{runState.CurrentActIndex} selection until ancient event finishes {DescribeCurrentEventState(runState)}");
			}
			else
			{
				HextechLog.Info($"[{ModInfo.Id}][Mayhem] StartRunDetour: selecting act{runState.CurrentActIndex} hex immediately after StartRun");
				await HextechRuneSelectionCoordinator.HandleActSelection(runState, modifier);
			}
		}
	}
}
