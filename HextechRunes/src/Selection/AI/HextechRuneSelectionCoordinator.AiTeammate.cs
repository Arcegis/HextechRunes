using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static partial class HextechRuneSelectionCoordinator
{
	private static async Task<IReadOnlyList<MonsterHexKind>> SelectRunesForAllPlayersAiTeammateHostControlled(
		RunState runState,
		HextechMayhemModifier modifier,
		int actIndex,
		HextechRarityTier rarity,
		IReadOnlyList<MonsterHexKind> previousMonsterHexes,
		IReadOnlyList<MonsterHexKind> initialNewMonsterHexes,
		RelicModel? monsterHexRelic)
	{
		Log.Info($"[{ModInfo.Id}][Mayhem][AITeammateCompat] Host-controlled rune selection started: act={actIndex}");
		IReadOnlyList<MonsterHexKind> initialActiveMonsterHexes = CombineMonsterHexes(previousMonsterHexes, initialNewMonsterHexes);
		List<(Player Player, List<RelicModel> Options)> selections = [];
		HashSet<ModelId> enemyRerollExcludedIdsForAllPlayers = new();
		foreach (Player player in runState.Players)
		{
			HashSet<ModelId> excludedIds = CreateBaseExcludedIds(modifier, player, initialActiveMonsterHexes);
			List<RelicModel> options = BuildStableSelectableRunesForRarity(
				player,
				rarity,
				runState,
				excludedIds,
				useEndlessTagWindow: modifier.IsEndlessLoopActive);
			enemyRerollExcludedIdsForAllPlayers.UnionWith(CreateEnemyHexRerollExcludedIds(options));
			selections.Add((player, options));
			Log.Info($"[{ModInfo.Id}][Mayhem][AITeammateCompat] Host-controlled options: player={player.NetId} ai={HextechAiTeammateCompat.IsAiPlayer(player)} count={options.Count} ids={string.Join(",", options.Select(o => (o.CanonicalInstance?.Id ?? o.Id).Entry))}");
		}

		List<MonsterHexKind> finalNewMonsterHexes = initialNewMonsterHexes.ToList();
		IReadOnlyList<MonsterHexKind> finalActiveMonsterHexes = initialActiveMonsterHexes;
		RelicModel? currentMonsterHexRelic = monsterHexRelic;
		bool enemyHexControlsUsed = false;
		HextechAiTeammateCompat.TryGetHostPlayerId(out ulong hostPlayerId);
		List<(Player Player, List<RelicModel> Options)> orderedSelections = selections
			.OrderBy(selection => hostPlayerId != 0UL
				? (selection.Player.NetId == hostPlayerId ? 0 : 1)
				: (HextechAiTeammateCompat.IsAiPlayer(selection.Player) ? 1 : 0))
			.ToList();
		foreach ((Player player, List<RelicModel> options) in orderedSelections)
		{
			bool isAiPlayer = HextechAiTeammateCompat.IsAiPlayer(player);
			bool canControlEnemyHex = !enemyHexControlsUsed
				&& (hostPlayerId == 0UL
					? !isAiPlayer
					: player.NetId == hostPlayerId);
			HextechEnemyHexAdjustmentOptions? enemyHexOptions = finalActiveMonsterHexes.Count > 0
				? new HextechEnemyHexAdjustmentOptions
				{
					InitialHexes = finalNewMonsterHexes,
					ExcludedHexes = finalActiveMonsterHexes,
					ControlsEnabled = canControlEnemyHex && finalNewMonsterHexes.Count > 0,
					RerollFunc = canControlEnemyHex && finalNewMonsterHexes.Count > 0
						? (currentHexes, slotIndex, rerollOrdinal) => RerollEnemyHexForAct(
							modifier,
							rarity,
							runState,
							actIndex,
							GetMonsterHexSlot(currentHexes, slotIndex),
							rerollOrdinal,
							CreateEnemyHexRerollExcludedIds(enemyRerollExcludedIdsForAllPlayers, currentHexes, slotIndex))
						: null
				}
				: null;
			RuneSelectionResult selection = await SelectRuneWithLocalScreen(
				modifier,
				player,
				options,
				currentMonsterHexRelic,
				enemyHexOptions,
				useMultiplayerReroll: true,
				removeOverlay: true,
				titleOverride: isAiPlayer
					? $"为{HextechAiTeammateCompat.GetDisplayName(player)}选择一个海克斯符文"
					: null);
			if (!IsCurrentRun(runState))
			{
				Log.Info($"[{ModInfo.Id}][Mayhem][AITeammateCompat] Host-controlled selection abort: stale run player={player.NetId}");
				return finalActiveMonsterHexes;
			}

			if (canControlEnemyHex)
			{
				enemyHexControlsUsed = true;
			}

			finalNewMonsterHexes = selection.ResolvedMonsterHexes.ToList();
			finalActiveMonsterHexes = CombineMonsterHexes(previousMonsterHexes, finalNewMonsterHexes);
			currentMonsterHexRelic = CreateMonsterHexRelic(FirstMonsterHexOrNull(finalNewMonsterHexes));
			RelicModel selectedRelic = selection.SelectedRelic ?? options[0];
			HextechTelemetry.RecordRuneChoice(runState, actIndex, rarity, player, selection.FinalOptions, selectedRelic, selection.RerollCount);
			await RelicCmd.Obtain(selectedRelic, player);
			Log.Info($"[{ModInfo.Id}][Mayhem][AITeammateCompat] Host-controlled obtained: player={player.NetId} ai={isAiPlayer} relic={(selectedRelic.CanonicalInstance?.Id ?? selectedRelic.Id).Entry}");
		}

		Log.Info($"[{ModInfo.Id}][Mayhem][AITeammateCompat] Host-controlled rune selection complete: act={actIndex} newMonsterHexes={string.Join(",", finalNewMonsterHexes)} activeMonsterHexes={string.Join(",", finalActiveMonsterHexes)}");
		return finalActiveMonsterHexes;
	}
}
