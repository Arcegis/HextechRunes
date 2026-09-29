using MegaCrit.Sts2.Core.Localization;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	internal static bool NeedsEnemyOnlySelection(int playerCount, int newEnemyCount, bool presetChallenge)
	{
		return playerCount <= 0 && newEnemyCount > 0 && !presetChallenge;
	}

	private static async Task<IReadOnlyList<MonsterHexKind>> SelectEnemyHexesOnly(
		RunState runState, HextechMayhemModifier modifier, int actIndex, HextechRarityTier rarity,
		IReadOnlyList<MonsterHexKind> previousHexes, IReadOnlyList<MonsterHexKind> newHexes,
		string? titleOverride = null)
	{
		RunManager manager = RunManager.Instance;
		EnemyHexAdjustmentSyncContext? sync = null;
		if (!HextechPlayerContextHelper.IsSinglePlayerFlow(manager.NetService.Type))
		{
			PlayerChoiceSynchronizer synchronizer = await WaitForPlayerChoiceSynchronizerAsync(manager);
			sync = CreateEnemyHexAdjustmentSyncContext(manager, runState, synchronizer, actIndex, newHexes)
				?? throw new OperationCanceledException("No enemy hex selection authority.");
		}

		bool authority = sync == null || IsLocalPlayer(manager, sync.AuthorityPlayer);
		using CancellationTokenSource cancellation = new();
		HextechRuneSelectionScreen? screen = null;
		try
		{
			HextechEnemyHexAdjustmentOptions options = CreateEnemyHexAdjustmentOptions(
				modifier,
				rarity,
				runState,
				actIndex,
				newHexes,
				controlsEnabled: authority,
				sync,
				cancellation.Token);
			// 空的玩家候选只显示敌方调整和确认，不抽取、同步或发放虚拟的玩家遗物。
			screen = await CreateRuneSelectionScreenAsync([], null, enemyHexOptions: options,
				titleOverride: titleOverride ?? new LocString(HextechRuneLabels.LocTable, "HEXTECH_ENEMY_PREVIEW_LABEL").GetRawText(),
				cancellationToken: cancellation.Token);
			if (!authority)
			{
				// 非权威端必有 sync,且推入界面时 ScreenCreated 已启动接收任务。
				await (sync?.RemoteReceiveTask
					?? throw new InvalidOperationException("Enemy hex adjustment receiver was not started."));
				if (!screen.EnemyOnlySelectionConfirmed)
				{
					throw new OperationCanceledException("Enemy hex selection interrupted before confirmation.");
				}
			}

			await screen.RelicsSelected(removeOverlay: false);
			if (authority && sync != null)
			{
				SendEnemyHexAdjustment(sync, screen.CurrentMonsterHexSlots, screen.EnemyHexRerollCounts, isFinal: true);
			}

			return screen.CurrentMonsterHexes;
		}
		finally
		{
			cancellation.Cancel();
			if (screen != null)
			{
				await screen.DismissAfterSelectionComplete();
			}

			await ObserveEnemyHexAdjustmentReceiveTask(sync);
		}
	}
}
