using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using static HextechRunes.HextechSelectionHelpers;

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
			HextechLog.Warn("RelicOptionChoice", $"No options available: player={player.NetId} context={context}");
			return null;
		}

		MarkRelicsSeen(options);
		return await HextechSyncedRelicChoice.SelectAsync(
			HextechSyncedRelicChoice.RelicOption,
			player,
			options,
			context,
			syncMultiplayerChoice,
			() => SelectLocalRelic(player, options, context));
	}

	private static async Task<RelicModel?> SelectLocalRelic(Player player, IReadOnlyList<RelicModel> options, string context)
	{
		try
		{
			if (!await WaitForOverlayStackAsync())
			{
				HextechLog.Warn("RelicOptionChoice", $"Overlay stack unavailable: player={player.NetId} context={context}");
				return null;
			}

			NChooseARelicSelection? screen = NChooseARelicSelection.ShowScreen(options);
			if (screen == null)
			{
				HextechLog.Warn("RelicOptionChoice", $"Selection screen unavailable: player={player.NetId} context={context}");
				return null;
			}

			RelicModel? selected = (await screen.RelicsSelected()).FirstOrDefault();
			HextechLog.Info("RelicOptionChoice", $"Local selected: player={player.NetId} relic={selected?.CanonicalId()?.Entry ?? "null"} context={context}");
			return selected;
		}
		catch (OperationCanceledException)
		{
			HextechLog.Info("RelicOptionChoice", $"Selection cancelled: player={player.NetId} context={context}");
			return null;
		}
	}

	private static async Task<bool> WaitForOverlayStackAsync()
	{
		return await WaitForSingletonAsync(static () => NOverlayStack.Instance) != null;
	}
}
