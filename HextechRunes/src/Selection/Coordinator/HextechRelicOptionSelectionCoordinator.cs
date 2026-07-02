using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace HextechRunes;

internal static class HextechRelicOptionSelectionCoordinator
{
	public static async Task<RelicModel?> SelectRelicOption(
		Player player,
		IReadOnlyList<RelicModel> options,
		string context,
		bool syncMultiplayerChoice = true)
	{
		if (options.Count == 0)
		{
			Log.Warn($"[{ModInfo.Id}][RelicOptionChoice] No options available: player={player.NetId} context={context}");
			return null;
		}

		MarkRelicsSeen(options);
		RunManager runManager = RunManager.Instance;
		NetGameType gameType = runManager.NetService.Type;
		if (gameType is NetGameType.Singleplayer or NetGameType.None)
		{
			return await SelectLocalRelic(player, options, context);
		}

		if (!syncMultiplayerChoice)
		{
			if (HextechRuneSelectionCoordinator.IsLocalPlayer(runManager, player))
			{
				return await SelectLocalRelic(player, options, context);
			}

			Log.Warn($"[{ModInfo.Id}][RelicOptionChoice] Unsynced relic option selection ignored for remote player={player.NetId} context={context}");
			return null;
		}

		PlayerChoiceSynchronizer? synchronizer = await HextechRuneSelectionCoordinator.WaitForPlayerChoiceSynchronizerAsync(runManager);
		if (synchronizer == null)
		{
			if (HextechRuneSelectionCoordinator.IsLocalPlayer(runManager, player))
			{
				return await SelectLocalRelic(player, options, context);
			}

			Log.Warn($"[{ModInfo.Id}][RelicOptionChoice] Choice synchronizer unavailable for remote player={player.NetId}; defaulting to first option context={context}");
			return options[0];
		}

		uint choiceId = synchronizer.ReserveChoiceId(player);
		if (HextechRuneSelectionCoordinator.IsLocalPlayer(runManager, player))
		{
			RelicModel? selected = await SelectLocalRelic(player, options, context);
			if (selected == null)
			{
				HextechLog.Info($"[{ModInfo.Id}][RelicOptionChoice] Local selection aborted: player={player.NetId} choiceId={choiceId} context={context}");
				return null;
			}

			int selectedIndex = IndexOfRelic(options, selected);
			if (selectedIndex < 0)
			{
				Log.Warn($"[{ModInfo.Id}][RelicOptionChoice] Local selection not in option set: player={player.NetId} context={context}");
				return null;
			}

			if (!runManager.NetService.IsConnected)
			{
				Log.Warn($"[{ModInfo.Id}][RelicOptionChoice] Local selection ignored because multiplayer service is disconnected: player={player.NetId} context={context}");
				return null;
			}

			PlayerChoiceResult result = HextechChoiceCodec.CreateRelicOptionSelection(selectedIndex, options);
			if (!HextechRuneSelectionCoordinator.TrySyncLocalHextechChoice(synchronizer, player, choiceId, result, $"relic-option-choice {context}", out uint sentChoiceId))
			{
				Log.Warn($"[{ModInfo.Id}][RelicOptionChoice] Sync local failed: player={player.NetId} choiceId={choiceId} index={selectedIndex} context={context}");
			}

			HextechLog.Info($"[{ModInfo.Id}][RelicOptionChoice] Sync local: player={player.NetId} choiceId={sentChoiceId} index={selectedIndex} context={context}");
			return selected;
		}

		HextechLog.Info($"[{ModInfo.Id}][RelicOptionChoice] Wait remote: player={player.NetId} choiceId={choiceId} context={context}");
		(PlayerChoiceResult remoteChoice, uint receivedChoiceId) = await HextechRuneSelectionCoordinator.WaitForRemoteHextechChoice(
			synchronizer,
			(RunState)player.RunState,
			player,
			choiceId,
			result => HextechChoiceCodec.IsRelicOptionSelection(result, options),
			$"relic-option-choice {context}");
		HextechLog.Info($"[{ModInfo.Id}][RelicOptionChoice] Remote received: player={player.NetId} choiceId={receivedChoiceId} context={context}");
		return ResolveRemoteRelicOptionChoice(player, options, remoteChoice, context);
	}

	private static async Task<RelicModel?> SelectLocalRelic(Player player, IReadOnlyList<RelicModel> options, string context)
	{
		try
		{
			if (!await WaitForOverlayStackAsync())
			{
				Log.Warn($"[{ModInfo.Id}][RelicOptionChoice] Overlay stack unavailable: player={player.NetId} context={context}");
				return null;
			}

			NChooseARelicSelection? screen = NChooseARelicSelection.ShowScreen(options);
			if (screen == null)
			{
				Log.Warn($"[{ModInfo.Id}][RelicOptionChoice] Selection screen unavailable: player={player.NetId} context={context}");
				return null;
			}

			RelicModel? selected = (await screen.RelicsSelected()).FirstOrDefault();
			HextechLog.Info($"[{ModInfo.Id}][RelicOptionChoice] Local selected: player={player.NetId} relic={(selected?.CanonicalInstance?.Id ?? selected?.Id)?.Entry ?? "null"} context={context}");
			return selected;
		}
		catch (OperationCanceledException)
		{
			HextechLog.Info($"[{ModInfo.Id}][RelicOptionChoice] Selection cancelled: player={player.NetId} context={context}");
			return null;
		}
	}

	private static async Task<bool> WaitForOverlayStackAsync()
	{
		for (int i = 0; i < 60; i++)
		{
			if (NOverlayStack.Instance != null)
			{
				return true;
			}

			await Task.Yield();
		}

		return NOverlayStack.Instance != null;
	}

	private static RelicModel ResolveRemoteRelicOptionChoice(Player player, IReadOnlyList<RelicModel> fallbackOptions, PlayerChoiceResult remoteChoice, string context)
	{
		if (!HextechChoiceCodec.TryDecodeRelicOptionSelection(remoteChoice, out int selectedIndex, out List<ModelId> optionIds))
		{
			Log.Warn($"[{ModInfo.Id}][RelicOptionChoice] Malformed payload: player={player.NetId} context={context} result={remoteChoice}");
			return fallbackOptions[0];
		}

		IReadOnlyList<RelicModel> finalOptions = fallbackOptions;
		if (optionIds.Count > 0)
		{
			try
			{
				finalOptions = optionIds.Select(static id => ModelDb.GetById<RelicModel>(id)).ToList();
			}
			catch (Exception ex)
			{
				Log.Warn($"[{ModInfo.Id}][RelicOptionChoice] Failed to load synced option model; falling back to local options: player={player.NetId} context={context} error={ex.Message}");
			}
		}

		if (selectedIndex < 0 || selectedIndex >= finalOptions.Count)
		{
			Log.Warn($"[{ModInfo.Id}][RelicOptionChoice] Invalid selected index: player={player.NetId} index={selectedIndex} count={finalOptions.Count} context={context}");
			return finalOptions.FirstOrDefault() ?? fallbackOptions[0];
		}

		return finalOptions[selectedIndex];
	}

	private static int IndexOfRelic(IReadOnlyList<RelicModel> options, RelicModel? selected)
	{
		if (selected == null)
		{
			return -1;
		}

		ModelId selectedId = selected.CanonicalInstance?.Id ?? selected.Id;
		for (int i = 0; i < options.Count; i++)
		{
			ModelId optionId = options[i].CanonicalInstance?.Id ?? options[i].Id;
			if (optionId == selectedId)
			{
				return i;
			}
		}

		return -1;
	}

	private static void MarkRelicsSeen(IReadOnlyList<RelicModel> relics)
	{
		foreach (RelicModel relic in relics)
		{
			SaveManager.Instance.MarkRelicAsSeen(relic);
		}
	}
}
