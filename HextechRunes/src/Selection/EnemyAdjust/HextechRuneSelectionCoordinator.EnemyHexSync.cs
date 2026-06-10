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
		MonsterHexKind? initialMonsterHex)
	{
		Player? authorityPlayer = GetActRollAuthorityPlayer(runManager, runState);
		if (authorityPlayer == null)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] EnemyHexAdjustmentSync: no authority player act={actIndex}");
			return null;
		}

		uint choiceId = synchronizer.ReserveChoiceId(authorityPlayer);
		Log.Info($"[{ModInfo.Id}][Mayhem] EnemyHexAdjustmentSync: reserved act={actIndex} authority={authorityPlayer.NetId} choiceId={choiceId}");
		return new EnemyHexAdjustmentSyncContext(synchronizer, authorityPlayer, choiceId, actIndex, initialMonsterHex);
	}

	private static HextechEnemyHexAdjustmentOptions? CreateEnemyHexAdjustmentOptionsForSelection(
		HextechMayhemModifier modifier,
		RunManager runManager,
		RunState runState,
		int actIndex,
		HextechRarityTier rarity,
		MonsterHexKind? initialMonsterHex,
		IReadOnlySet<ModelId> enemyRerollExcludedIds,
		EnemyHexAdjustmentSyncContext? syncContext,
		PendingRuneSelection selection)
	{
		if (!selection.IsLocal)
		{
			return null;
		}

		bool isAuthorityLocal = syncContext != null && IsLocalPlayer(runManager, syncContext.AuthorityPlayer);
		return new HextechEnemyHexAdjustmentOptions
		{
			InitialHex = syncContext?.CurrentMonsterHex ?? initialMonsterHex,
			ControlsEnabled = isAuthorityLocal,
			RerollFunc = isAuthorityLocal
				? (currentHex, rerollOrdinal) => RerollEnemyHexForAct(modifier, rarity, runState, actIndex, currentHex, rerollOrdinal, enemyRerollExcludedIds)
				: null,
			Changed = isAuthorityLocal && syncContext != null
				? (monsterHex, removed, rerollCount) => SendEnemyHexAdjustment(syncContext, monsterHex, removed, rerollCount, isFinal: false)
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
			SendEnemyHexAdjustment(syncContext, screen.CurrentMonsterHex, screen.EnemyHexRemoved, screen.EnemyHexRerollCount, isFinal: true);
			return;
		}

		if (syncContext.RemoteReceiveTask != null)
		{
			await syncContext.RemoteReceiveTask;
		}
	}

	private static void SendEnemyHexAdjustment(
		EnemyHexAdjustmentSyncContext syncContext,
		MonsterHexKind? monsterHex,
		bool removed,
		int rerollCount,
		bool isFinal)
	{
		if (syncContext.FinalSent)
		{
			return;
		}

		syncContext.CurrentMonsterHex = removed ? null : monsterHex;
		syncContext.Removed = removed;
		syncContext.RerollCount = rerollCount;
		EnemyHexAdjustmentPayload payload = new(
			syncContext.ActIndex,
			syncContext.Sequence,
			monsterHex,
			removed,
			rerollCount,
			isFinal);
		syncContext.Synchronizer.SyncLocalChoice(syncContext.AuthorityPlayer, syncContext.NextChoiceId, HextechChoiceCodec.CreateEnemyHexAdjustment(payload));
		Log.Info($"[{ModInfo.Id}][Mayhem] EnemyHexAdjustmentSync send: act={syncContext.ActIndex} choiceId={syncContext.NextChoiceId} seq={syncContext.Sequence} removed={removed} hex={monsterHex} rerolls={rerollCount} final={isFinal}");
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

			syncContext.CurrentMonsterHex = payload.Removed ? null : payload.MonsterHex;
			syncContext.Removed = payload.Removed;
			syncContext.RerollCount = payload.RerollCount;
			syncContext.Sequence = payload.Sequence + 1;
			screen.ApplyEnemyHexAdjustment(payload.MonsterHex, payload.Removed, payload.RerollCount);
			Log.Info($"[{ModInfo.Id}][Mayhem] EnemyHexAdjustmentSync receive: act={syncContext.ActIndex} choiceId={receivedChoiceId} seq={payload.Sequence} removed={payload.Removed} hex={payload.MonsterHex} rerolls={payload.RerollCount} final={payload.IsFinal}");
			if (payload.IsFinal)
			{
				return;
			}

			syncContext.NextChoiceId = syncContext.Synchronizer.ReserveChoiceId(syncContext.AuthorityPlayer);
		}
	}
}
