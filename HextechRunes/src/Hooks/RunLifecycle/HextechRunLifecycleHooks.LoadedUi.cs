using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static partial class HextechRunLifecycleHooks
{
	private const int EnemyUiRefreshFrameBudget = 45;

	private static void LoadRunPostfix(RunState runState, ref Task __result)
	{
		__result = LoadRunAfterOriginal(__result, runState);
	}

	private static async Task LoadRunAfterOriginal(Task original, RunState runState)
	{
		await original;
		await RefreshEnemyUiForRunWhenReady(runState, "LoadRun", EnemyUiRefreshFrameBudget);
	}

	private static void TopBarInitializePostfix(IRunState runState)
	{
		if (runState is RunState concreteRunState)
		{
			ScheduleEnemyUiRefresh(concreteRunState, "NTopBar.Initialize", EnemyUiRefreshFrameBudget);
			return;
		}

		ScheduleEnemyUiRefreshForCurrentRun("NTopBar.Initialize", EnemyUiRefreshFrameBudget);
	}

	private static void ScheduleEnemyUiRefresh(RunState runState, string reason, int frameBudget)
	{
		TaskHelper.RunSafely(RefreshEnemyUiForRunWhenReady(runState, reason, frameBudget));
	}

	private static void ScheduleEnemyUiRefreshForCurrentRun(string reason, int frameBudget)
	{
		TaskHelper.RunSafely(RefreshEnemyUiForCurrentRunWhenReady(reason, frameBudget));
	}

	private static async Task RefreshEnemyUiForCurrentRunWhenReady(string reason, int frameBudget)
	{
		for (int frame = 0; frame <= frameBudget; frame++)
		{
			if (RunManager.Instance.DebugOnlyGetState() is RunState runState)
			{
				bool refreshed = TryRefreshEnemyUiForRun(runState, reason, frame);
				if (refreshed)
				{
					return;
				}
			}

			await WaitOneFrame();
		}

		HextechEnemyUi.HideMayhemModifierBadge();
		Log.Info($"[{ModInfo.Id}][Mayhem] EnemyUi delayed refresh skipped: reason={reason} no current run after {frameBudget} frames");
	}

	private static async Task RefreshEnemyUiForRunWhenReady(RunState runState, string reason, int frameBudget)
	{
		for (int frame = 0; frame <= frameBudget; frame++)
		{
			if (TryRefreshEnemyUiForRun(runState, reason, frame))
			{
				return;
			}

			await WaitOneFrame();
		}

		HextechEnemyUi.HideMayhemModifierBadge();
		Log.Info($"[{ModInfo.Id}][Mayhem] EnemyUi delayed refresh skipped: reason={reason} topbar/modifier not ready after {frameBudget} frames");
	}

	private static bool TryRefreshEnemyUiForRun(RunState runState, string reason, int frame)
	{
		if (!IsCurrentRun(runState))
		{
			return true;
		}

		if (NRun.Instance?.GlobalUi?.TopBar == null || !HextechEnemyUi.IsTopBarReady())
		{
			return false;
		}

		HextechMayhemModifier? modifier = GetMayhemModifier(runState);
		if (modifier == null)
		{
			HextechEnemyUi.HideMayhemModifierBadge();
			return false;
		}

		SubscribeRoomEnteredIfNeeded();
		SubscribeRoomExitedIfNeeded();
		bool recovered = !modifier.IsActResolved(runState.CurrentActIndex)
			&& modifier.TryRecoverResolvedActsFromPlayerRelics(reason);
		HextechEnemyUi.Refresh(modifier);
		Log.Info($"[{ModInfo.Id}][Mayhem] EnemyUi delayed refresh: reason={reason} frame={frame} recovered={recovered} actIndex={runState.CurrentActIndex} {modifier.DescribeActState()}");
		return true;
	}
}
