using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	private HashSet<string>? _playerRuneConfigDisabledIds;

	internal bool HasPlayerRuneConfigDisabledIdsSnapshot => _playerRuneConfigDisabledIds != null;

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
		_playerRuneConfigDisabledIds = CreateNewRunPlayerRuneConfigDisabledIdsSnapshot();
		Log.Info($"[{ModInfo.Id}][Mayhem] Player rune config snapshot initialized: reason={reason} disabled={_playerRuneConfigDisabledIds.Count}");
	}

	internal void SetPlayerRuneConfigDisabledIdsSnapshot(IEnumerable<string>? disabledIds, string reason)
	{
		_playerRuneConfigDisabledIds = HextechRuneConfiguration.NormalizeDisabledPlayerRuneIds(disabledIds);
		Log.Info($"[{ModInfo.Id}][Mayhem] Player rune config snapshot set: reason={reason} disabled={_playerRuneConfigDisabledIds.Count}");
	}

	private HashSet<string> GetPlayerRuneConfigDisabledIdsForPool()
	{
		if (_playerRuneConfigDisabledIds != null)
		{
			return _playerRuneConfigDisabledIds.ToHashSet(StringComparer.Ordinal);
		}

		try
		{
			if (RunManager.Instance.NetService.Type == NetGameType.Client)
			{
				return new HashSet<string>(StringComparer.Ordinal);
			}
		}
		catch
		{
			// Fall back to local config outside a fully initialized multiplayer run.
		}

		return HextechRuneConfiguration.GetDisabledPlayerRuneIds().ToHashSet(StringComparer.Ordinal);
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
		if (_playerRuneConfigDisabledIds == null)
		{
			return "";
		}

		string[] ids = _playerRuneConfigDisabledIds
			.OrderBy(static id => id, StringComparer.Ordinal)
			.ToArray();
		return JsonSerializer.Serialize(ids, HextechTelemetry.JsonOptions);
	}

	private void RestorePlayerRuneConfigDisabledIds(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			_playerRuneConfigDisabledIds = null;
			return;
		}

		try
		{
			string[]? ids = JsonSerializer.Deserialize<string[]>(json, HextechTelemetry.JsonOptions);
			_playerRuneConfigDisabledIds = HextechRuneConfiguration.NormalizeDisabledPlayerRuneIds(ids);
		}
		catch (Exception ex)
		{
			_playerRuneConfigDisabledIds = null;
			Log.Warn($"[{ModInfo.Id}][Mayhem] Player rune config snapshot restore failed; using runtime fallback: {ex.Message}", 2);
		}
	}
}
