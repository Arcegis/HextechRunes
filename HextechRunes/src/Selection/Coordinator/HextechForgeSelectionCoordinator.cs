using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using static HextechRunes.HextechSelectionHelpers;

namespace HextechRunes;

internal static class HextechForgeSelectionCoordinator
{
	private const string LocTable = "relic_collection";

	public static async Task<RelicModel?> SelectForge(Player player, IReadOnlyList<RelicModel> options, string context, bool syncMultiplayerChoice = true)
	{
		if (options.Count == 0)
		{
			Log.Warn($"[{ModInfo.Id}][ForgeChoice] No forge options available: player={player.NetId} context={context}");
			return null;
		}

		MarkRelicsSeen(options);

		// 杂项配置「随机获得锻造器」开启时,跳过三选一界面,直接从同一候选池稳定随机给一个。
		// 用 HextechStableRandom 基于 RunState 种子决定,所有客户端独立算出同一结果;reward 路径
		// 又只在选择方执行并经 RewardSynchronizer 广播,因此无需 PlayerChoiceSynchronizer 往返,联机一致。
		// 配置经 RunConfigurationSnapshot 跟随主机,故双端要么都短路要么都不短路。
		if (ShouldDirectlyGrantRandomForge(player))
		{
			RelicModel directGrant = PickStableRandomForge(player, options, context);
			HextechLog.Info($"[{ModInfo.Id}][ForgeChoice] Random direct grant (choice skipped): player={player.NetId} relic={(directGrant.CanonicalInstance?.Id ?? directGrant.Id).Entry} context={context}");
			return directGrant;
		}

		RunManager runManager = RunManager.Instance;
		NetGameType gameType = runManager.NetService.Type;
		if (gameType is NetGameType.Singleplayer or NetGameType.None)
		{
			return await SelectLocalForge(player, options, context);
		}

		if (!syncMultiplayerChoice)
		{
			if (HextechRuneSelectionCoordinator.IsLocalPlayer(runManager, player))
			{
				return await SelectLocalForge(player, options, context);
			}

			Log.Warn($"[{ModInfo.Id}][ForgeChoice] Unsynced forge selection ignored for remote player={player.NetId} context={context}");
			return null;
		}

		PlayerChoiceSynchronizer? synchronizer = await HextechRuneSelectionCoordinator.WaitForPlayerChoiceSynchronizerAsync(runManager);
		if (synchronizer == null)
		{
			if (HextechRuneSelectionCoordinator.IsLocalPlayer(runManager, player))
			{
				return await SelectLocalForge(player, options, context);
			}

			Log.Warn($"[{ModInfo.Id}][ForgeChoice] Choice synchronizer unavailable for remote player={player.NetId}; defaulting to first option context={context}");
			return options[0];
		}

		uint choiceId = synchronizer.ReserveChoiceId(player);
		if (HextechRuneSelectionCoordinator.IsLocalPlayer(runManager, player))
		{
			RelicModel? selected = await SelectLocalForge(player, options, context);
			if (selected == null)
			{
				HextechLog.Info($"[{ModInfo.Id}][ForgeChoice] Local selection aborted: player={player.NetId} choiceId={choiceId} context={context}");
				return null;
			}

			int selectedIndex = IndexOfRelicById(options, selected);
			if (selectedIndex < 0)
			{
				Log.Warn($"[{ModInfo.Id}][ForgeChoice] Local selection not in option set: player={player.NetId} context={context}");
				return null;
			}

			if (!runManager.NetService.IsConnected)
			{
				Log.Warn($"[{ModInfo.Id}][ForgeChoice] Local selection ignored because multiplayer service is disconnected: player={player.NetId} context={context}");
				return null;
			}

			if (!HextechRuneSelectionCoordinator.TrySyncLocalHextechChoice(synchronizer, player, choiceId, HextechChoiceCodec.CreateForgeSelection(selectedIndex, options), $"forge-choice {context}", out uint sentChoiceId))
			{
				Log.Warn($"[{ModInfo.Id}][ForgeChoice] Sync local failed: player={player.NetId} choiceId={choiceId} index={selectedIndex} context={context}");
			}

			HextechLog.Info($"[{ModInfo.Id}][ForgeChoice] Sync local: player={player.NetId} choiceId={sentChoiceId} index={selectedIndex} context={context}");
			return selected;
		}

		HextechLog.Info($"[{ModInfo.Id}][ForgeChoice] Wait remote: player={player.NetId} choiceId={choiceId} context={context}");
		(PlayerChoiceResult remoteChoice, uint receivedChoiceId) = await HextechRuneSelectionCoordinator.WaitForRemoteHextechChoice(
			synchronizer,
			(RunState)player.RunState,
			player,
			choiceId,
			HextechChoiceCodec.IsForgeSelection,
			$"forge-choice {context}",
			HextechChoiceCodec.IsMalformedForgeSelectionEnvelope);
		HextechLog.Info($"[{ModInfo.Id}][ForgeChoice] Remote received: player={player.NetId} choiceId={receivedChoiceId} context={context}");
		return ResolveRemoteForgeChoice(player, options, remoteChoice, context);
	}

	private static async Task<RelicModel?> SelectLocalForge(Player player, IReadOnlyList<RelicModel> options, string context)
	{
		try
		{
			HextechRuneSelectionScreen screen = await CreateForgeSelectionScreenAsync(options);
			RelicModel? selected = (await screen.RelicsSelected()).FirstOrDefault();
			HextechLog.Info($"[{ModInfo.Id}][ForgeChoice] Local selected: player={player.NetId} relic={(selected?.CanonicalInstance?.Id ?? selected?.Id)?.Entry ?? "null"} context={context}");
			return selected;
		}
		catch (OperationCanceledException)
		{
			HextechLog.Info($"[{ModInfo.Id}][ForgeChoice] Selection cancelled: player={player.NetId} context={context}");
			return null;
		}
	}

	private static async Task<HextechRuneSelectionScreen> CreateForgeSelectionScreenAsync(IReadOnlyList<RelicModel> options)
	{
		await WaitForSingletonAsync(static () => NOverlayStack.Instance);
		HextechRuneSelectionScreen screen = HextechRuneSelectionScreen.Create(
			options,
			monsterHexRelic: null,
			rerollFunc: null,
			enemyHexOptions: null,
			titleOverride: new LocString(LocTable, "HEXTECH_FORGE_SELECTION_TITLE").GetRawText(),
			metadataMode: HextechSelectionMetadataMode.Forge);
		if (NOverlayStack.Instance == null)
		{
			throw new InvalidOperationException("NOverlayStack is not available for forge selection.");
		}

		NOverlayStack.Instance.Push(screen);
		return screen;
	}

	private static RelicModel ResolveRemoteForgeChoice(Player player, IReadOnlyList<RelicModel> fallbackOptions, PlayerChoiceResult remoteChoice, string context)
	{
		string payloadDump = HextechChoiceCodec.TryGetIndexPayload(remoteChoice, out List<int> payload)
			? $"[{string.Join(",", payload)}]"
			: remoteChoice.ToString();
		if (!HextechChoiceCodec.TryDecodeForgeSelection(remoteChoice, out int selectedIndex, out List<ModelId> optionIds))
		{
			string message = $"[{ModInfo.Id}][ForgeChoice] Malformed payload: player={player.NetId} context={context} payload={payloadDump}";
			Log.Error(message);
			throw new InvalidOperationException(message);
		}

		IReadOnlyList<RelicModel> finalOptions = fallbackOptions;
		if (optionIds.Count > 0)
		{
			try
			{
				finalOptions = optionIds.Select(id => ModelDb.GetById<RelicModel>(id).ToMutable()).ToList();
			}
			catch (Exception ex)
			{
				Log.Error($"[{ModInfo.Id}][ForgeChoice] Failed to load synced option model; falling back to local options: player={player.NetId} selectedIndex={selectedIndex} context={context} ids={string.Join(",", optionIds)} error={ex}");
			}
		}

		if (selectedIndex < 0 || selectedIndex >= finalOptions.Count)
		{
			string message = $"[{ModInfo.Id}][ForgeChoice] Invalid selected index: player={player.NetId} index={selectedIndex} count={finalOptions.Count} context={context} payload={payloadDump}";
			Log.Error(message);
			throw new InvalidOperationException(message);
		}

		return finalOptions[selectedIndex];
	}

	private static bool ShouldDirectlyGrantRandomForge(Player player)
	{
		try
		{
			if (player.RunState is RunState runState
				&& runState.Modifiers.OfType<HextechMayhemModifier>().LastOrDefault() is HextechMayhemModifier modifier)
			{
				return modifier.RandomForgeDirectGrant;
			}
		}
		catch (Exception ex)
		{
			Log.Error($"[{ModInfo.Id}][ForgeChoice] Failed to read synchronized random-forge setting; using deterministic false fallback: player={player.NetId} error={ex}");
			return false;
		}

		return HextechRuneConfiguration.GetSnapshot().RandomForgeDirectGrant;
	}

	private static RelicModel PickStableRandomForge(Player player, IReadOnlyList<RelicModel> options, string context)
	{
		int index = HextechStableRandom.Index(
			(RunState)player.RunState,
			options.Count,
			"forge-direct-grant",
			HextechStableRandom.PlayerKey(player),
			context,
			player.Relics.Count.ToString());
		return options[Math.Clamp(index, 0, options.Count - 1)];
	}

}
