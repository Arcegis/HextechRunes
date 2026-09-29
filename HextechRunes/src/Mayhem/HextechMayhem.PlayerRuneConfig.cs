namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	internal bool HasPlayerRuneConfigDisabledIdsSnapshot => _runContext.PlayerRuneConfig.HasSnapshot;

	internal IReadOnlySet<string> PlayerRuneConfigDisabledIds => GetPlayerRuneConfigDisabledIdsForPool();

	internal void SetPlayerRuneConfigDisabledIdsSnapshot(IEnumerable<string>? disabledIds, string reason)
	{
		_runContext.PlayerRuneConfig.Set(disabledIds);
		HextechLog.Info("Mayhem", $"Player rune config snapshot set: reason={reason} disabled={_runContext.PlayerRuneConfig.SnapshotCount}");
	}

	private HashSet<string> GetPlayerRuneConfigDisabledIdsForPool()
	{
		return _runContext.PlayerRuneConfig.GetDisabledIdsForPool(
			HextechPlayerContextHelper.IsClientRun(),
			HextechRuneConfiguration.GetDisabledPlayerRuneIds());
	}

	private string SerializePlayerRuneConfigDisabledIds()
	{
		return _runContext.PlayerRuneConfig.Serialize();
	}

	private void RestorePlayerRuneConfigDisabledIds(string json)
	{
		if (!_runContext.PlayerRuneConfig.TryRestore(json, out string? errorMessage))
		{
			HextechLog.Warn("Mayhem", $"Player rune config snapshot restore failed; using runtime fallback: {errorMessage}");
		}
	}
}
