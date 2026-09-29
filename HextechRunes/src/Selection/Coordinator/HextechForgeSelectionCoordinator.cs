using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using static HextechRunes.HextechSelectionHelpers;

namespace HextechRunes;

internal static class HextechForgeSelectionCoordinator
{
	public static async Task<RelicModel?> SelectForge(Player player, IReadOnlyList<RelicModel> options, string context, bool syncMultiplayerChoice = true)
	{
		if (options.Count > HextechStableModelIdListCodec.MaxCount)
		{
			throw new ArgumentOutOfRangeException(
				nameof(options),
				options.Count,
				$"Forge option count must not exceed {HextechStableModelIdListCodec.MaxCount}.");
		}

		if (options.Count == 0)
		{
			HextechLog.Warn("ForgeChoice", $"No forge options available: player={player.NetId} context={context}");
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
			HextechLog.Info("ForgeChoice", $"Random direct grant (choice skipped): player={player.NetId} relic={(directGrant.CanonicalId()).Entry} context={context}");
			return directGrant;
		}

		return await HextechSyncedRelicChoice.SelectAsync(
			HextechSyncedRelicChoice.Forge,
			player,
			options,
			context,
			syncMultiplayerChoice,
			() => SelectLocalForge(player, options, context));
	}

	private static async Task<RelicModel?> SelectLocalForge(Player player, IReadOnlyList<RelicModel> options, string context)
	{
		try
		{
			HextechRuneSelectionScreen screen = await CreateForgeSelectionScreenAsync(options);
			RelicModel? selected = (await screen.RelicsSelected()).FirstOrDefault();
			HextechLog.Info("ForgeChoice", $"Local selected: player={player.NetId} relic={(selected?.CanonicalId())?.Entry ?? "null"} context={context}");
			return selected;
		}
		catch (OperationCanceledException)
		{
			HextechLog.Info("ForgeChoice", $"Selection cancelled: player={player.NetId} context={context}");
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
			titleOverride: new LocString(HextechRuneLabels.LocTable, "HEXTECH_FORGE_SELECTION_TITLE").GetRawText(),
			metadataMode: HextechSelectionMetadataMode.Forge);
		if (NOverlayStack.Instance == null)
		{
			throw new InvalidOperationException("NOverlayStack is not available for forge selection.");
		}

		NOverlayStack.Instance.Push(screen);
		return screen;
	}

	private static bool ShouldDirectlyGrantRandomForge(Player player)
	{
		try
		{
			if (player.RunState is RunState runState
				&& HextechMayhemModifier.FindIn(runState) is HextechMayhemModifier modifier)
			{
				return modifier.RandomForgeDirectGrant;
			}

			// 没有本局 modifier(外部模组在未启用本模组的局里调用 API 等)时与 HextechMayhemModifier.IsEnabledForRun
			// 同一口径:联机缺少本局快照,不能用各端本地菜单值决定共享结果,一律不直发;单机才读本地配置。
			return !HextechPlayerContextHelper.IsNetworkMultiplayerRun()
				&& HextechRuneConfiguration.GetSnapshot().RandomForgeDirectGrant;
		}
		catch (Exception ex)
		{
			HextechLog.Error("ForgeChoice", $"Failed to read synchronized random-forge setting; using deterministic false fallback: player={player.NetId} error={ex}");
			return false;
		}
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
