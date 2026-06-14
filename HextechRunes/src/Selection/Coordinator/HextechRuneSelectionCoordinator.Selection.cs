using Godot;
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
	private static async Task<RuneSelectionResult> SelectRune(
		HextechMayhemModifier modifier,
		Player player,
		IReadOnlyList<RelicModel> options,
		RelicModel? monsterHexRelic,
		HextechEnemyHexAdjustmentOptions? enemyHexOptions = null)
	{
		RunManager runManager = RunManager.Instance;
		NetGameType gameType = runManager.NetService.Type;
		if (gameType is NetGameType.Singleplayer or NetGameType.None)
		{
			MarkRelicsSeen(options);
			modifier.RecordSeenPlayerRunes(player, options);
			HashSet<ModelId> seenOptionIds = CreateSeenOptionIds(options, monsterHexRelic, modifier.GetSeenPlayerRuneIds(player));
			AddMonsterHexIconIds(seenOptionIds, GetEnemyHexesExcludedFromPlayerRerolls(enemyHexOptions));
			HextechRuneSelectionScreen screen = await CreateRuneSelectionScreenAsync(
				options,
				monsterHexRelic,
				(relics, slotIndex, _) => RerollSingleOptionAndTrack(modifier, player, relics, slotIndex, seenOptionIds),
				enemyHexOptions);
			RelicModel? selectedRelic = (await screen.RelicsSelected()).FirstOrDefault();
			return new RuneSelectionResult(selectedRelic, screen.CurrentRelics.ToList(), screen.RerollHistory.Count, screen.CurrentMonsterHex, screen.CurrentMonsterHexes);
		}

		PlayerChoiceSynchronizer? synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);
		if (synchronizer == null)
		{
			MarkRelicsSeen(options);
			modifier.RecordSeenPlayerRunes(player, options);
			RelicModel? selectedRelic = await RelicSelectCmd.FromChooseARelicScreen(player, options);
			return new RuneSelectionResult(selectedRelic, options.ToList(), 0, FirstMonsterHexOrNull(enemyHexOptions?.InitialHexes), enemyHexOptions?.InitialHexes);
		}

		uint choiceId = synchronizer.ReserveChoiceId(player);
		if (IsLocalPlayer(runManager, player))
		{
			MarkRelicsSeen(options);
			modifier.RecordSeenPlayerRunes(player, options);
			HashSet<ModelId> seenOptionIds = CreateSeenOptionIds(options, monsterHexRelic, modifier.GetSeenPlayerRuneIds(player));
			AddMonsterHexIconIds(seenOptionIds, GetEnemyHexesExcludedFromPlayerRerolls(enemyHexOptions));
			HextechRuneSelectionScreen screen = await CreateRuneSelectionScreenAsync(
				options,
				monsterHexRelic,
				(relics, slotIndex, rerollOrdinal) => RerollSingleOptionAndTrackMultiplayer(modifier, player, relics, slotIndex, rerollOrdinal, seenOptionIds),
				enemyHexOptions);
			RelicModel? selectedRelic = (await screen.RelicsSelected()).FirstOrDefault();
			synchronizer.SyncLocalChoice(player, choiceId, CreateRuneChoiceResult(screen, selectedRelic));
			Log.Info($"[{ModInfo.Id}][Mayhem] RuneChoice sync local: player={player.NetId} choiceId={choiceId}");
			return new RuneSelectionResult(selectedRelic, screen.CurrentRelics.ToList(), screen.RerollHistory.Count, screen.CurrentMonsterHex, screen.CurrentMonsterHexes);
		}

		if (HextechAiTeammateCompat.ShouldAutoSelectRune(player))
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] RuneChoice AI auto-select: player={player.NetId} choiceId={choiceId}");
			MarkRelicsSeen(options);
			modifier.RecordSeenPlayerRunes(player, options);
			int selectedIndex = HextechAiTeammateCompat.PickRandomRuneIndex(player, options);
			RelicModel? selectedRelic = selectedIndex >= 0 && selectedIndex < options.Count ? options[selectedIndex] : null;
			return new RuneSelectionResult(selectedRelic, options.ToList(), 0, null);
		}

		Log.Info($"[{ModInfo.Id}][Mayhem] RuneChoice wait remote: player={player.NetId} choiceId={choiceId}");
		(PlayerChoiceResult remoteChoice, uint receivedChoiceId) = await WaitForRemoteHextechChoice(
			synchronizer,
			(RunState)player.RunState,
			player,
			choiceId,
			HextechChoiceCodec.IsRuneSelection,
			"rune-choice");
		Log.Info($"[{ModInfo.Id}][Mayhem] RuneChoice remote received: player={player.NetId} choiceId={receivedChoiceId}");
		return ResolveRemoteRuneChoice(modifier, player, options, remoteChoice, monsterHexRelic);
	}

	private static async Task<RuneSelectionResult> SelectRuneMultiplayer(
		HextechMayhemModifier modifier,
		PendingRuneSelection selection,
		PlayerChoiceSynchronizer synchronizer,
		RelicModel? monsterHexRelic,
		HextechEnemyHexAdjustmentOptions? enemyHexOptions = null,
		Func<HextechRuneSelectionScreen, Task>? afterLocalSelection = null)
	{
		if (selection.IsLocal)
		{
			MarkRelicsSeen(selection.Options);
			modifier.RecordSeenPlayerRunes(selection.Player, selection.Options);
			HashSet<ModelId> seenOptionIds = CreateSeenOptionIds(selection.Options, monsterHexRelic, modifier.GetSeenPlayerRuneIds(selection.Player));
			AddMonsterHexIconIds(seenOptionIds, GetEnemyHexesExcludedFromPlayerRerolls(enemyHexOptions));
			HextechRuneSelectionScreen screen = await CreateRuneSelectionScreenAsync(
				selection.Options,
				monsterHexRelic,
				(relics, slotIndex, rerollOrdinal) => RerollSingleOptionAndTrackMultiplayer(modifier, selection.Player, relics, slotIndex, rerollOrdinal, seenOptionIds),
				enemyHexOptions);
			RelicModel? selectedRelic = (await screen.RelicsSelected(removeOverlay: false)).FirstOrDefault();
			synchronizer.SyncLocalChoice(selection.Player, selection.ChoiceId, CreateRuneChoiceResult(screen, selectedRelic));
			Log.Info($"[{ModInfo.Id}][Mayhem] RuneChoice sync local: player={selection.Player.NetId} choiceId={selection.ChoiceId}");
			if (afterLocalSelection != null)
			{
				await afterLocalSelection(screen);
			}

			return new RuneSelectionResult(selectedRelic, screen.CurrentRelics.ToList(), screen.RerollHistory.Count, screen.CurrentMonsterHex, screen.CurrentMonsterHexes, screen);
		}

		if (HextechAiTeammateCompat.ShouldAutoSelectRune(selection.Player))
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] RuneChoice AI auto-select: player={selection.Player.NetId} choiceId={selection.ChoiceId}");
			int selectedIndex = HextechAiTeammateCompat.PickRandomRuneIndex(selection.Player, selection.Options);
			RelicModel? selectedRelic = selectedIndex >= 0 && selectedIndex < selection.Options.Count ? selection.Options[selectedIndex] : null;
			return new RuneSelectionResult(selectedRelic, selection.Options.ToList(), 0, null);
		}

		Log.Info($"[{ModInfo.Id}][Mayhem] RuneChoice wait remote: player={selection.Player.NetId} choiceId={selection.ChoiceId}");
		(PlayerChoiceResult remoteChoice, uint receivedChoiceId) = await WaitForRemoteHextechChoice(
			synchronizer,
			(RunState)selection.Player.RunState,
			selection.Player,
			selection.ChoiceId,
			HextechChoiceCodec.IsRuneSelection,
			"rune-choice");
		Log.Info($"[{ModInfo.Id}][Mayhem] RuneChoice remote received: player={selection.Player.NetId} choiceId={receivedChoiceId}");
		return ResolveRemoteRuneChoice(modifier, selection.Player, selection.Options, remoteChoice, monsterHexRelic);
	}

	private static async Task<HextechRuneSelectionScreen> CreateRuneSelectionScreenAsync(
		IReadOnlyList<RelicModel> relics,
		RelicModel? monsterHexRelic,
		Func<IReadOnlyList<RelicModel>, int, int, IReadOnlyList<RelicModel>>? rerollFunc = null,
		HextechEnemyHexAdjustmentOptions? enemyHexOptions = null,
		string? titleOverride = null)
	{
		for (int i = 0; i < 60; i++)
		{
			if (NOverlayStack.Instance != null)
			{
				break;
			}

			await Task.Yield();
		}

		HextechRuneSelectionScreen selectionScreen = HextechRuneSelectionScreen.Create(relics, monsterHexRelic, rerollFunc, enemyHexOptions, titleOverride);
		if (NOverlayStack.Instance == null)
		{
			throw new InvalidOperationException("NOverlayStack is not available for rune selection.");
		}

		NOverlayStack.Instance.Push(selectionScreen);
		enemyHexOptions?.ScreenCreated?.Invoke(selectionScreen);
		return selectionScreen;
	}

	private static async Task<RuneSelectionResult> SelectRuneWithLocalScreen(
		HextechMayhemModifier modifier,
		Player player,
		IReadOnlyList<RelicModel> options,
		RelicModel? monsterHexRelic,
		HextechEnemyHexAdjustmentOptions? enemyHexOptions,
		bool useMultiplayerReroll,
		bool removeOverlay,
		string? titleOverride = null)
	{
		MarkRelicsSeen(options);
		modifier.RecordSeenPlayerRunes(player, options);
		HashSet<ModelId> seenOptionIds = CreateSeenOptionIds(options, monsterHexRelic, modifier.GetSeenPlayerRuneIds(player));
		AddMonsterHexIconIds(seenOptionIds, GetEnemyHexesExcludedFromPlayerRerolls(enemyHexOptions));
		HextechRuneSelectionScreen screen = await CreateRuneSelectionScreenAsync(
			options,
			monsterHexRelic,
			useMultiplayerReroll
				? (relics, slotIndex, rerollOrdinal) => RerollSingleOptionAndTrackMultiplayer(modifier, player, relics, slotIndex, rerollOrdinal, seenOptionIds)
				: (relics, slotIndex, _) => RerollSingleOptionAndTrack(modifier, player, relics, slotIndex, seenOptionIds),
			enemyHexOptions,
			titleOverride);
		RelicModel? selectedRelic = (await screen.RelicsSelected(removeOverlay)).FirstOrDefault();
		return new RuneSelectionResult(selectedRelic, screen.CurrentRelics.ToList(), screen.RerollHistory.Count, screen.CurrentMonsterHex, screen.CurrentMonsterHexes, removeOverlay ? null : screen);
	}

	private static PlayerChoiceResult CreateRuneChoiceResult(HextechRuneSelectionScreen screen, RelicModel? selectedRelic)
	{
		int selectedIndex = selectedRelic == null ? -1 : IndexOfRelic(screen.CurrentRelics, selectedRelic);
		Log.Info($"[{ModInfo.Id}][Mayhem] CreateRuneChoiceResult: selectedIndex={selectedIndex} rerolls={string.Join(",", screen.RerollHistory)}");
		return HextechChoiceCodec.CreateRuneSelection(selectedIndex, screen.RerollHistory, screen.CurrentRelics);
	}

	private static int IndexOfRelic(IReadOnlyList<RelicModel> relics, RelicModel relic)
	{
		for (int i = 0; i < relics.Count; i++)
		{
			if (ReferenceEquals(relics[i], relic))
			{
				return i;
			}
		}

		return -1;
	}

	private static RuneSelectionResult ResolveRemoteRuneChoice(HextechMayhemModifier modifier, Player player, IReadOnlyList<RelicModel> options, PlayerChoiceResult remoteChoice, RelicModel? monsterHexRelic)
	{
		if (!HextechChoiceCodec.TryDecodeRuneSelection(remoteChoice, out int selectedIndex, out List<int> rerollHistory, out List<ModelId> syncedOptionIds))
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] ResolveRemoteRuneChoice: malformed hextech rune payload player={player.NetId} result={remoteChoice}");
			return new RuneSelectionResult(null, options.ToList(), 0, null);
		}

		if (syncedOptionIds.Count > 0)
		{
			if (TryCreateSyncedRuneOptions(player, syncedOptionIds, out List<RelicModel> syncedOptions))
			{
				MarkRelicsSeen(syncedOptions);
				modifier.RecordSeenPlayerRunes(player, syncedOptions);
				RelicModel? syncedSelectedRelic = selectedIndex >= 0 && selectedIndex < syncedOptions.Count ? syncedOptions[selectedIndex] : null;
				Log.Info($"[{ModInfo.Id}][Mayhem] ResolveRemoteRuneChoice: player={player.NetId} selectedIndex={selectedIndex} rerolls={string.Join(",", rerollHistory)} syncedOptions={string.Join(",", syncedOptions.Select(o => (o.CanonicalInstance?.Id ?? o.Id).Entry))}");
				return new RuneSelectionResult(syncedSelectedRelic, syncedOptions, rerollHistory.Count, null);
			}

			Log.Warn($"[{ModInfo.Id}][Mayhem] ResolveRemoteRuneChoice: failed to create synced options; falling back to deterministic replay player={player.NetId} ids={string.Join(",", syncedOptionIds.Select(static id => id.Entry))}");
		}

		MarkRelicsSeen(options);
		modifier.RecordSeenPlayerRunes(player, options);
		HashSet<ModelId> seenOptionIds = CreateSeenOptionIds(options, monsterHexRelic, modifier.GetSeenPlayerRuneIds(player));
		IReadOnlyList<RelicModel> currentOptions = options;
		for (int i = 0; i < rerollHistory.Count; i++)
		{
			int slotIndex = rerollHistory[i];
			currentOptions = RerollSingleOptionAndTrackMultiplayer(modifier, player, currentOptions, slotIndex, i, seenOptionIds);
		}

		Log.Info($"[{ModInfo.Id}][Mayhem] ResolveRemoteRuneChoice: player={player.NetId} selectedIndex={selectedIndex} rerolls={string.Join(",", rerollHistory)}");
		RelicModel? selectedRelic = selectedIndex >= 0 && selectedIndex < currentOptions.Count ? currentOptions[selectedIndex] : null;
		return new RuneSelectionResult(selectedRelic, currentOptions.ToList(), rerollHistory.Count, null);
	}

	private static IEnumerable<MonsterHexKind>? GetEnemyHexesExcludedFromPlayerRerolls(HextechEnemyHexAdjustmentOptions? enemyHexOptions)
	{
		if (enemyHexOptions == null)
		{
			return null;
		}

		return enemyHexOptions.ExcludedHexes.Count > 0
			? enemyHexOptions.ExcludedHexes
			: enemyHexOptions.InitialHexes;
	}

	private static bool TryCreateSyncedRuneOptions(Player player, IReadOnlyList<ModelId> optionIds, out List<RelicModel> options)
	{
		options = new(optionIds.Count);
		try
		{
			foreach (ModelId id in optionIds)
			{
				RelicModel relic = ModelDb.GetById<RelicModel>(id);
				options.Add(CreateSelectableRuneOption(player, relic));
			}

			return options.Count > 0;
		}
		catch (Exception ex)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] ResolveRemoteRuneChoice: failed to load synced option model: {ex.Message}", 2);
			options.Clear();
			return false;
		}
	}
}
