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
		int actIndex,
		int choiceOrdinal,
		IReadOnlyList<RelicModel> options,
		RelicModel? monsterHexRelic,
		HextechEnemyHexAdjustmentOptions? enemyHexOptions = null)
	{
		string context = $"rune-choice act={actIndex} ordinal={choiceOrdinal}";
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
				enemyHexOptions,
				modifier.PlayerRuneRerollLimit);
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
				enemyHexOptions,
				modifier.PlayerRuneRerollLimit);
			RelicModel? selectedRelic = (await screen.RelicsSelected()).FirstOrDefault();
			if (TrySyncLocalHextechChoice(synchronizer, player, choiceId, CreateRuneChoiceResult(actIndex, choiceOrdinal, screen, selectedRelic), context, out uint sentChoiceId))
			{
				HextechLog.Info($"[{ModInfo.Id}][Mayhem] RuneChoice sync local: act={actIndex} ordinal={choiceOrdinal} player={player.NetId} choiceId={sentChoiceId}");
			}
			else
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem] RuneChoice sync local failed: act={actIndex} ordinal={choiceOrdinal} player={player.NetId} choiceId={choiceId}");
			}
			return new RuneSelectionResult(selectedRelic, screen.CurrentRelics.ToList(), screen.RerollHistory.Count, screen.CurrentMonsterHex, screen.CurrentMonsterHexes);
		}

		if (HextechAiTeammateCompat.ShouldAutoSelectRune(player))
		{
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] RuneChoice AI auto-select: act={actIndex} ordinal={choiceOrdinal} player={player.NetId} choiceId={choiceId}");
			MarkRelicsSeen(options);
			modifier.RecordSeenPlayerRunes(player, options);
			int selectedIndex = HextechAiTeammateCompat.PickRandomRuneIndex(player, options);
			RelicModel? selectedRelic = selectedIndex >= 0 && selectedIndex < options.Count ? options[selectedIndex] : null;
			return new RuneSelectionResult(selectedRelic, options.ToList(), 0, null);
		}

		HextechLog.Info($"[{ModInfo.Id}][Mayhem] RuneChoice wait remote: act={actIndex} ordinal={choiceOrdinal} player={player.NetId} choiceId={choiceId}");
		(PlayerChoiceResult remoteChoice, uint receivedChoiceId)? received = await TryWaitForRemoteHextechChoice(
			synchronizer,
			(RunState)player.RunState,
			player,
			choiceId,
			result => HextechChoiceCodec.IsRuneSelection(result, actIndex, choiceOrdinal),
			context,
			RemoteRuneChoicePollFrames,
			() => ShouldKeepWaitingForRemoteRuneChoice((RunState)player.RunState));
		if (!received.HasValue)
		{
			return CreateRemoteRuneChoiceFallback(modifier, player, options, context, choiceId);
		}

		(PlayerChoiceResult remoteChoice, uint receivedChoiceId) = received.Value;
		HextechLog.Info($"[{ModInfo.Id}][Mayhem] RuneChoice remote received: act={actIndex} ordinal={choiceOrdinal} player={player.NetId} choiceId={receivedChoiceId}");
		return ResolveRemoteRuneChoice(modifier, player, actIndex, choiceOrdinal, options, remoteChoice, monsterHexRelic);
	}

	private static async Task<RuneSelectionResult> SelectRuneMultiplayer(
		HextechMayhemModifier modifier,
		PendingRuneSelection selection,
		PlayerChoiceSynchronizer synchronizer,
		int actIndex,
		int choiceOrdinal,
		RelicModel? monsterHexRelic,
		HextechEnemyHexAdjustmentOptions? enemyHexOptions = null,
		Func<HextechRuneSelectionScreen, Task>? afterLocalSelection = null)
	{
		string context = $"rune-choice act={actIndex} ordinal={choiceOrdinal}";
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
				enemyHexOptions,
				modifier.PlayerRuneRerollLimit);
			RelicModel? selectedRelic = (await screen.RelicsSelected(removeOverlay: false)).FirstOrDefault();
			if (TrySyncLocalHextechChoice(synchronizer, selection.Player, selection.ChoiceId, CreateRuneChoiceResult(actIndex, choiceOrdinal, screen, selectedRelic), context, out uint sentChoiceId))
			{
				HextechLog.Info($"[{ModInfo.Id}][Mayhem] RuneChoice sync local: act={actIndex} ordinal={choiceOrdinal} player={selection.Player.NetId} choiceId={sentChoiceId}");
			}
			else
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem] RuneChoice sync local failed: act={actIndex} ordinal={choiceOrdinal} player={selection.Player.NetId} choiceId={selection.ChoiceId}");
			}
			if (afterLocalSelection != null)
			{
				await afterLocalSelection(screen);
			}

			return new RuneSelectionResult(selectedRelic, screen.CurrentRelics.ToList(), screen.RerollHistory.Count, screen.CurrentMonsterHex, screen.CurrentMonsterHexes, screen);
		}

		if (HextechAiTeammateCompat.ShouldAutoSelectRune(selection.Player))
		{
			HextechLog.Info($"[{ModInfo.Id}][Mayhem] RuneChoice AI auto-select: act={actIndex} ordinal={choiceOrdinal} player={selection.Player.NetId} choiceId={selection.ChoiceId}");
			int selectedIndex = HextechAiTeammateCompat.PickRandomRuneIndex(selection.Player, selection.Options);
			RelicModel? selectedRelic = selectedIndex >= 0 && selectedIndex < selection.Options.Count ? selection.Options[selectedIndex] : null;
			return new RuneSelectionResult(selectedRelic, selection.Options.ToList(), 0, null);
		}

		HextechLog.Info($"[{ModInfo.Id}][Mayhem] RuneChoice wait remote: act={actIndex} ordinal={choiceOrdinal} player={selection.Player.NetId} choiceId={selection.ChoiceId}");
		(PlayerChoiceResult remoteChoice, uint receivedChoiceId)? received = await TryWaitForRemoteHextechChoice(
			synchronizer,
			(RunState)selection.Player.RunState,
			selection.Player,
			selection.ChoiceId,
			result => HextechChoiceCodec.IsRuneSelection(result, actIndex, choiceOrdinal),
			context,
			RemoteRuneChoicePollFrames,
			() => ShouldKeepWaitingForRemoteRuneChoice((RunState)selection.Player.RunState));
		if (!received.HasValue)
		{
			return CreateRemoteRuneChoiceFallback(modifier, selection.Player, selection.Options, context, selection.ChoiceId);
		}

		(PlayerChoiceResult remoteChoice, uint receivedChoiceId) = received.Value;
		HextechLog.Info($"[{ModInfo.Id}][Mayhem] RuneChoice remote received: act={actIndex} ordinal={choiceOrdinal} player={selection.Player.NetId} choiceId={receivedChoiceId}");
		return ResolveRemoteRuneChoice(modifier, selection.Player, actIndex, choiceOrdinal, selection.Options, remoteChoice, monsterHexRelic);
	}

	private static bool ShouldKeepWaitingForRemoteRuneChoice(RunState runState)
	{
		return IsCurrentRun(runState) && IsMultiplayerConnected();
	}

	private static async Task<HextechRuneSelectionScreen> CreateRuneSelectionScreenAsync(
		IReadOnlyList<RelicModel> relics,
		RelicModel? monsterHexRelic,
		Func<IReadOnlyList<RelicModel>, int, int, IReadOnlyList<RelicModel>>? rerollFunc = null,
		HextechEnemyHexAdjustmentOptions? enemyHexOptions = null,
		int playerRuneRerollLimit = 1,
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

		HextechRuneSelectionScreen selectionScreen = HextechRuneSelectionScreen.Create(relics, monsterHexRelic, rerollFunc, enemyHexOptions, playerRuneRerollLimit, titleOverride);
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
			modifier.PlayerRuneRerollLimit,
			titleOverride);
		RelicModel? selectedRelic = (await screen.RelicsSelected(removeOverlay)).FirstOrDefault();
		return new RuneSelectionResult(selectedRelic, screen.CurrentRelics.ToList(), screen.RerollHistory.Count, screen.CurrentMonsterHex, screen.CurrentMonsterHexes, removeOverlay ? null : screen);
	}

	private static PlayerChoiceResult CreateRuneChoiceResult(int actIndex, int choiceOrdinal, HextechRuneSelectionScreen screen, RelicModel? selectedRelic)
	{
		int selectedIndex = selectedRelic == null ? -1 : IndexOfRelic(screen.CurrentRelics, selectedRelic);
		HextechLog.Info($"[{ModInfo.Id}][Mayhem] CreateRuneChoiceResult: act={actIndex} ordinal={choiceOrdinal} selectedIndex={selectedIndex} rerolls={string.Join(",", screen.RerollHistory)}");
		return HextechChoiceCodec.CreateRuneSelection(actIndex, choiceOrdinal, selectedIndex, screen.RerollHistory, screen.CurrentRelics);
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

	private static RuneSelectionResult CreateRemoteRuneChoiceFallback(
		HextechMayhemModifier modifier,
		Player player,
		IReadOnlyList<RelicModel> options,
		string context,
		uint choiceId)
	{
		MarkRelicsSeen(options);
		modifier.RecordSeenPlayerRunes(player, options);
		RelicModel? selectedRelic = options.FirstOrDefault();
		string selectedId = selectedRelic == null ? "None" : (selectedRelic.CanonicalInstance?.Id ?? selectedRelic.Id).Entry;
		Log.Warn($"[{ModInfo.Id}][Mayhem] RuneChoice fallback: context={context} player={player.NetId} choiceId={choiceId} selected={selectedId}");
		return new RuneSelectionResult(selectedRelic, options.ToList(), 0, null);
	}

	private static RuneSelectionResult ResolveRemoteRuneChoice(
		HextechMayhemModifier modifier,
		Player player,
		int actIndex,
		int choiceOrdinal,
		IReadOnlyList<RelicModel> options,
		PlayerChoiceResult remoteChoice,
		RelicModel? monsterHexRelic)
	{
		if (!HextechChoiceCodec.TryDecodeRuneSelection(remoteChoice, actIndex, choiceOrdinal, out int selectedIndex, out List<int> rerollHistory, out List<ModelId> syncedOptionIds))
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] ResolveRemoteRuneChoice: malformed hextech rune payload act={actIndex} ordinal={choiceOrdinal} player={player.NetId} result={remoteChoice}");
			return new RuneSelectionResult(null, options.ToList(), 0, null);
		}

		if (syncedOptionIds.Count > 0)
		{
			if (TryCreateSyncedRuneOptions(player, syncedOptionIds, out List<RelicModel> syncedOptions))
			{
				MarkRelicsSeen(syncedOptions);
				modifier.RecordSeenPlayerRunes(player, syncedOptions);
				RelicModel? syncedSelectedRelic = selectedIndex >= 0 && selectedIndex < syncedOptions.Count ? syncedOptions[selectedIndex] : null;
				HextechLog.Info($"[{ModInfo.Id}][Mayhem] ResolveRemoteRuneChoice: player={player.NetId} selectedIndex={selectedIndex} rerolls={string.Join(",", rerollHistory)} syncedOptions={string.Join(",", syncedOptions.Select(o => (o.CanonicalInstance?.Id ?? o.Id).Entry))}");
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

		HextechLog.Info($"[{ModInfo.Id}][Mayhem] ResolveRemoteRuneChoice: player={player.NetId} selectedIndex={selectedIndex} rerolls={string.Join(",", rerollHistory)}");
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
