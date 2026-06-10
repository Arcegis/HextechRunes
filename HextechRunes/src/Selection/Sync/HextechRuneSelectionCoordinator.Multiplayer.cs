using Godot;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	private static async Task<MonsterHexKind?> SelectRunesForAllPlayersMultiplayer(
		RunState runState,
		HextechMayhemModifier modifier,
		int actIndex,
		HextechRarityTier rarity,
		MonsterHexKind? initialMonsterHex,
		RelicModel? monsterHexRelic)
	{
		RunManager runManager = RunManager.Instance;
		if (HextechAiTeammateCompat.IsLoopbackHostSession()
			&& runState.Players.Any(static player => HextechAiTeammateCompat.IsAiPlayer(player)))
		{
			return await SelectRunesForAllPlayersAiTeammateHostControlled(runState, modifier, actIndex, rarity, initialMonsterHex, monsterHexRelic);
		}

		PlayerChoiceSynchronizer? synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);
		if (synchronizer == null)
		{
			MonsterHexKind? fallbackMonsterHex = initialMonsterHex;
			foreach (Player player in runState.Players)
			{
				HashSet<ModelId> excludedIds = CreateBaseExcludedIds(modifier, player, monsterHexRelic);
				List<RelicModel> options = BuildStableSelectableRunesForRarity(
					player,
					rarity,
					runState,
					excludedIds,
					useEndlessTagWindow: modifier.IsEndlessLoopActive);
				HashSet<ModelId> enemyRerollExcludedIds = CreateEnemyHexRerollExcludedIds(options);
				RuneSelectionResult selection = await SelectRune(
					modifier,
					player,
					options,
					monsterHexRelic,
					new HextechEnemyHexAdjustmentOptions
					{
						InitialHex = fallbackMonsterHex,
						ControlsEnabled = runManager.NetService.Type == NetGameType.Host && IsLocalPlayer(runManager, player),
						RerollFunc = (currentHex, rerollOrdinal) => RerollEnemyHexForAct(modifier, rarity, runState, actIndex, currentHex, rerollOrdinal, enemyRerollExcludedIds)
					});
				fallbackMonsterHex = selection.FinalMonsterHex;
				monsterHexRelic = CreateMonsterHexRelic(fallbackMonsterHex);
				RelicModel selected = selection.SelectedRelic ?? options[0];
				HextechTelemetry.RecordRuneChoice(runState, actIndex, rarity, player, selection.FinalOptions, selected, selection.RerollCount);
				await RelicCmd.Obtain(selected, player);
			}

			return fallbackMonsterHex;
		}

		EnemyHexAdjustmentSyncContext? enemyHexSync = CreateEnemyHexAdjustmentSyncContext(runManager, runState, synchronizer, actIndex, initialMonsterHex);
		HashSet<ModelId> enemyRerollExcludedIdsForAllPlayers = new();
		List<PendingRuneSelection> pendingSelections = [];
		foreach (Player player in runState.Players)
		{
			HashSet<ModelId> excludedIds = CreateBaseExcludedIds(modifier, player, monsterHexRelic);
			List<RelicModel> options = BuildStableSelectableRunesForRarity(
				player,
				rarity,
				runState,
				excludedIds,
				useEndlessTagWindow: modifier.IsEndlessLoopActive);
			enemyRerollExcludedIdsForAllPlayers.UnionWith(CreateEnemyHexRerollExcludedIds(options));
			MarkRelicsSeen(options);
			modifier.RecordSeenPlayerRunes(player, options);

			uint choiceId = synchronizer.ReserveChoiceId(player);
			pendingSelections.Add(new PendingRuneSelection(player, options, choiceId, IsLocalPlayer(runManager, player)));
			Log.Info($"[{ModInfo.Id}][Mayhem] RuneChoice pending: player={player.NetId} choiceId={choiceId} local={IsLocalPlayer(runManager, player)} options={string.Join(",", options.Select(o => (o.CanonicalInstance?.Id ?? o.Id).Entry))}");
		}

		RuneSelectionResult[] selectedRelics = [];
		try
		{
			selectedRelics = await Task.WhenAll(pendingSelections.Select(selection =>
				SelectRuneMultiplayer(
					modifier,
					selection,
					synchronizer,
					monsterHexRelic,
					CreateEnemyHexAdjustmentOptionsForSelection(
						modifier,
						runManager,
						runState,
						actIndex,
						rarity,
						initialMonsterHex,
						enemyRerollExcludedIdsForAllPlayers,
						enemyHexSync,
						selection),
					screen => CompleteLocalEnemyHexAdjustmentSync(runManager, enemyHexSync, screen))));
			for (int i = 0; i < pendingSelections.Count; i++)
			{
				PendingRuneSelection selection = pendingSelections[i];
				RuneSelectionResult selectedResult = selectedRelics[i];
				RelicModel selectedRelic = selectedResult.SelectedRelic ?? selectedResult.FinalOptions.FirstOrDefault() ?? selection.Options[0];
				HextechTelemetry.RecordRuneChoice(runState, actIndex, rarity, selection.Player, selectedResult.FinalOptions, selectedRelic, selectedResult.RerollCount);
			}

			await SynchronizeActSelectionApplied(runState, synchronizer, actIndex);

			for (int i = 0; i < pendingSelections.Count; i++)
			{
				PendingRuneSelection selection = pendingSelections[i];
				RuneSelectionResult selectedResult = selectedRelics[i];
				RelicModel selectedRelic = selectedResult.SelectedRelic ?? selectedResult.FinalOptions.FirstOrDefault() ?? selection.Options[0];
				await RelicCmd.Obtain(selectedRelic, selection.Player);
			}

			return enemyHexSync != null ? enemyHexSync.CurrentMonsterHex : initialMonsterHex;
		}
		finally
		{
			await DismissBlockingSelectionScreens(selectedRelics);
		}
	}

	private static async Task DismissBlockingSelectionScreens(IEnumerable<RuneSelectionResult> selections)
	{
		foreach (HextechRuneSelectionScreen screen in selections
			.Select(static selection => selection.BlockingScreen)
			.Where(static screen => screen != null)
			.Distinct()
			.Cast<HextechRuneSelectionScreen>())
		{
			await screen.DismissAfterSelectionComplete();
		}
	}

}
