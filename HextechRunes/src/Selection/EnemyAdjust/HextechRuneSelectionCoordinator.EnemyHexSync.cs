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

	private static bool SendEnemyHexAdjustment(
		EnemyHexAdjustmentSyncContext syncContext,
		IReadOnlyList<MonsterHexKind?> monsterHexes,
		IReadOnlyList<int> rerollCounts,
		bool isFinal)
	{
		if (syncContext.FinalSent)
		{
			return true;
		}

		List<MonsterHexKind?> nextMonsterHexes = monsterHexes.ToList();
		List<int> nextRerollCounts = rerollCounts.Select(static count => Math.Max(0, count)).ToList();
		EnemyHexAdjustmentPayload payload = new(
			syncContext.ActIndex,
			syncContext.Sequence,
			nextMonsterHexes.ToArray(),
			nextRerollCounts.ToArray(),
			isFinal);
		if (!TrySyncLocalHextechChoice(syncContext.Synchronizer, syncContext.AuthorityPlayer, syncContext.NextChoiceId, HextechChoiceCodec.CreateEnemyHexAdjustment(payload), $"enemy-hex-adjustment act={syncContext.ActIndex}", out uint sentChoiceId))
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] EnemyHexAdjustmentSync send failed: act={syncContext.ActIndex} choiceId={syncContext.NextChoiceId} seq={syncContext.Sequence} final={isFinal}");
			return false;
		}

		syncContext.CurrentMonsterHexSlots.Clear();
		syncContext.CurrentMonsterHexSlots.AddRange(nextMonsterHexes);
		syncContext.RerollCounts.Clear();
		syncContext.RerollCounts.AddRange(nextRerollCounts);
		Log.Info($"[{ModInfo.Id}][Mayhem] EnemyHexAdjustmentSync send: act={syncContext.ActIndex} choiceId={sentChoiceId} seq={syncContext.Sequence} hexes={string.Join(",", syncContext.CurrentMonsterHexSlots.Select(static hex => hex?.ToString() ?? "None"))} rerolls={string.Join(",", syncContext.RerollCounts)} final={isFinal}");
		if (isFinal)
		{
			syncContext.FinalSent = true;
			return true;
		}

		syncContext.Sequence++;
		syncContext.NextChoiceId = syncContext.Synchronizer.ReserveChoiceId(syncContext.AuthorityPlayer);
		return true;
	}

	private static async Task ReceiveEnemyHexAdjustments(EnemyHexAdjustmentSyncContext syncContext, RunState runState, HextechRuneSelectionScreen screen)
	{
		while (screen.IsInsideTree())
		{
			(PlayerChoiceResult result, uint receivedChoiceId)? received = await TryWaitForRemoteHextechChoice(
				syncContext.Synchronizer,
				runState,
				syncContext.AuthorityPlayer,
				syncContext.NextChoiceId,
				choice => HextechChoiceCodec.TryDecodeEnemyHexAdjustment(choice, syncContext.ActIndex, out _),
				$"enemy-hex-adjustment act={syncContext.ActIndex}",
				EnemyHexAdjustmentTimeoutFrames);
			if (!received.HasValue)
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem] EnemyHexAdjustmentSync timeout: act={syncContext.ActIndex} choiceId={syncContext.NextChoiceId}");
				return;
			}

			(PlayerChoiceResult result, uint receivedChoiceId) = received.Value;
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
