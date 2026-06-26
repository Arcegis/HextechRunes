using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	internal bool HasPlayerRuneConfigDisabledIdsSnapshot => _runContext.PlayerRuneConfig.HasSnapshot;

	internal IReadOnlySet<string> PlayerRuneConfigDisabledIds => GetPlayerRuneConfigDisabledIdsForPool();

	internal bool HasDisabledPlayerRunesForPool => GetPlayerRuneConfigDisabledIdsForPool().Count > 0;

	internal bool IsPlayerRuneEnabledForPool(RelicModel relic)
	{
		ModelId id = relic.CanonicalInstance?.Id ?? relic.Id;
		return IsPlayerRuneEnabledForPool(id.Entry);
	}

	internal bool IsPlayerRuneEnabledForPool(string id)
	{
		return !GetPlayerRuneConfigDisabledIdsForPool().Contains(id);
	}

	internal void InitializePlayerRuneConfigDisabledIdsSnapshotForNewRun(string reason)
	{
		_runContext.PlayerRuneConfig.Set(CreateNewRunPlayerRuneConfigDisabledIdsSnapshot());
		HextechLog.Info($"[{ModInfo.Id}][Mayhem] Player rune config snapshot initialized: reason={reason} disabled={_runContext.PlayerRuneConfig.SnapshotCount}");
	}

	internal void SetPlayerRuneConfigDisabledIdsSnapshot(IEnumerable<string>? disabledIds, string reason)
	{
		_runContext.PlayerRuneConfig.Set(disabledIds);
		HextechLog.Info($"[{ModInfo.Id}][Mayhem] Player rune config snapshot set: reason={reason} disabled={_runContext.PlayerRuneConfig.SnapshotCount}");
	}

	private HashSet<string> GetPlayerRuneConfigDisabledIdsForPool()
	{
		bool isClient = false;
		try
		{
			isClient = RunManager.Instance.NetService.Type == NetGameType.Client;
		}
		catch
		{
			// Fall back to local config outside a fully initialized multiplayer run.
		}

		return _runContext.PlayerRuneConfig.GetDisabledIdsForPool(
			isClient,
			HextechRuneConfiguration.GetDisabledPlayerRuneIds());
	}

	private static HashSet<string> CreateNewRunPlayerRuneConfigDisabledIdsSnapshot()
	{
		try
		{
			return RunManager.Instance.NetService.Type == NetGameType.Client
				? new HashSet<string>(StringComparer.Ordinal)
				: HextechRuneConfiguration.GetDisabledPlayerRuneIds().ToHashSet(StringComparer.Ordinal);
		}
		catch
		{
			return HextechRuneConfiguration.GetDisabledPlayerRuneIds().ToHashSet(StringComparer.Ordinal);
		}
	}

	private string SerializePlayerRuneConfigDisabledIds()
	{
		return _runContext.PlayerRuneConfig.Serialize();
	}

	private void RestorePlayerRuneConfigDisabledIds(string json)
	{
		if (!_runContext.PlayerRuneConfig.TryRestore(json, out string? errorMessage))
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] Player rune config snapshot restore failed; using runtime fallback: {errorMessage}", 2);
		}
	}
}
