using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	private static EnemyHexAdjustmentSyncContext? CreateEnemyHexAdjustmentSyncContext(
		RunManager runManager,
		RunState runState,
		PlayerChoiceSynchronizer synchronizer,
		int actIndex,
		IReadOnlyList<MonsterHexKind> initialMonsterHexes)
	{
		Player? authorityPlayer = GetActRollAuthorityPlayer(runManager, runState);
		if (authorityPlayer == null)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] EnemyHexAdjustmentSync: no authority player act={actIndex}");
			return null;
		}

		uint choiceId = synchronizer.ReserveChoiceId(authorityPlayer);
		Log.Info($"[{ModInfo.Id}][Mayhem] EnemyHexAdjustmentSync: reserved act={actIndex} authority={authorityPlayer.NetId} choiceId={choiceId}");
		return new EnemyHexAdjustmentSyncContext(synchronizer, authorityPlayer, choiceId, actIndex, initialMonsterHexes);
	}

	private static HextechEnemyHexAdjustmentOptions? CreateEnemyHexAdjustmentOptionsForSelection(
		HextechMayhemModifier modifier,
		RunManager runManager,
		RunState runState,
		int actIndex,
		HextechRarityTier rarity,
		IReadOnlyList<MonsterHexKind> activeMonsterHexes,
		IReadOnlyList<MonsterHexKind> initialNewMonsterHexes,
		IReadOnlySet<ModelId> enemyRerollExcludedIds,
		EnemyHexAdjustmentSyncContext? syncContext,
		PendingRuneSelection selection)
	{
		if (!selection.IsLocal || (syncContext == null && activeMonsterHexes.Count == 0))
		{
			return null;
		}

		bool isAuthorityLocal = syncContext != null && IsLocalPlayer(runManager, syncContext.AuthorityPlayer);
		return new HextechEnemyHexAdjustmentOptions
		{
			InitialHexes = syncContext?.CurrentMonsterHexes ?? initialNewMonsterHexes,
			ExcludedHexes = activeMonsterHexes,
			ControlsEnabled = isAuthorityLocal,
			RerollFunc = isAuthorityLocal
				? (currentHexes, slotIndex, rerollOrdinal) => RerollEnemyHexForAct(
					modifier,
					rarity,
					runState,
					actIndex,
					GetMonsterHexSlot(currentHexes, slotIndex),
					rerollOrdinal,
					CreateEnemyHexRerollExcludedIds(enemyRerollExcludedIds, currentHexes, slotIndex))
				: null,
			Changed = isAuthorityLocal && syncContext != null
				? (monsterHexes, rerollCounts) => SendEnemyHexAdjustment(syncContext, monsterHexes, rerollCounts, isFinal: false)
				: null,
			ScreenCreated = !isAuthorityLocal && syncContext != null
				? screen => syncContext.RemoteReceiveTask = ReceiveEnemyHexAdjustments(syncContext, runState, screen)
				: null
		};
	}

	private static async Task CompleteLocalEnemyHexAdjustmentSync(RunManager runManager, EnemyHexAdjustmentSyncContext? syncContext, HextechRuneSelectionScreen screen)
	{
		if (syncContext == null)
		{
			return;
		}

		if (IsLocalPlayer(runManager, syncContext.AuthorityPlayer))
		{
			SendEnemyHexAdjustment(syncContext, screen.CurrentMonsterHexSlots, screen.EnemyHexRerollCounts, isFinal: true);
			return;
		}

		if (syncContext.RemoteReceiveTask != null)
		{
			await syncContext.RemoteReceiveTask;
		}
	}

	private static void SendEnemyHexAdjustment(
		EnemyHexAdjustmentSyncContext syncContext,
		IReadOnlyList<MonsterHexKind?> monsterHexes,
		IReadOnlyList<int> rerollCounts,
		bool isFinal)
	{
		if (syncContext.FinalSent)
		{
			return;
		}

		syncContext.CurrentMonsterHexSlots.Clear();
		syncContext.CurrentMonsterHexSlots.AddRange(monsterHexes);
		syncContext.RerollCounts.Clear();
		syncContext.RerollCounts.AddRange(rerollCounts.Select(static count => Math.Max(0, count)));
		EnemyHexAdjustmentPayload payload = new(
			syncContext.ActIndex,
			syncContext.Sequence,
			syncContext.CurrentMonsterHexSlots.ToArray(),
			syncContext.RerollCounts.ToArray(),
			isFinal);
		syncContext.Synchronizer.SyncLocalChoice(syncContext.AuthorityPlayer, syncContext.NextChoiceId, HextechChoiceCodec.CreateEnemyHexAdjustment(payload));
		Log.Info($"[{ModInfo.Id}][Mayhem] EnemyHexAdjustmentSync send: act={syncContext.ActIndex} choiceId={syncContext.NextChoiceId} seq={syncContext.Sequence} hexes={string.Join(",", syncContext.CurrentMonsterHexSlots.Select(static hex => hex?.ToString() ?? "None"))} rerolls={string.Join(",", syncContext.RerollCounts)} final={isFinal}");
		if (isFinal)
		{
			syncContext.FinalSent = true;
			return;
		}

		syncContext.Sequence++;
		syncContext.NextChoiceId = syncContext.Synchronizer.ReserveChoiceId(syncContext.AuthorityPlayer);
	}

	private static async Task ReceiveEnemyHexAdjustments(EnemyHexAdjustmentSyncContext syncContext, RunState runState, HextechRuneSelectionScreen screen)
	{
		while (screen.IsInsideTree())
		{
			(PlayerChoiceResult result, uint receivedChoiceId) = await WaitForRemoteHextechChoice(
				syncContext.Synchronizer,
				runState,
				syncContext.AuthorityPlayer,
				syncContext.NextChoiceId,
				choice => HextechChoiceCodec.TryDecodeEnemyHexAdjustment(choice, syncContext.ActIndex, out _),
				$"enemy-hex-adjustment act={syncContext.ActIndex}");
			if (!HextechChoiceCodec.TryDecodeEnemyHexAdjustment(result, syncContext.ActIndex, out EnemyHexAdjustmentPayload payload))
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem] EnemyHexAdjustmentSync malformed: act={syncContext.ActIndex} choiceId={receivedChoiceId}");
				return;
			}

			syncContext.CurrentMonsterHexSlots.Clear();
			syncContext.CurrentMonsterHexSlots.AddRange(payload.MonsterHexes);
			syncContext.RerollCounts.Clear();
			syncContext.RerollCounts.AddRange(payload.RerollCounts.Select(static count => Math.Max(0, count)));
			syncContext.Sequence = payload.Sequence + 1;
			screen.ApplyEnemyHexAdjustment(payload.MonsterHexes, payload.RerollCounts);
			Log.Info($"[{ModInfo.Id}][Mayhem] EnemyHexAdjustmentSync receive: act={syncContext.ActIndex} choiceId={receivedChoiceId} seq={payload.Sequence} hexes={string.Join(",", payload.MonsterHexes.Select(static hex => hex?.ToString() ?? "None"))} rerolls={string.Join(",", payload.RerollCounts)} final={payload.IsFinal}");
			if (payload.IsFinal)
			{
				return;
			}

			syncContext.NextChoiceId = syncContext.Synchronizer.ReserveChoiceId(syncContext.AuthorityPlayer);
		}
	}
}
