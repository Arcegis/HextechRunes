using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	public int[] PlayerHexCountsByAct => _runContext.PlayerHexCounts.Snapshot;

	internal IReadOnlySet<string> DisabledMonsterHexIdsForPool => GetEffectiveRunConfigurationSnapshot().DisabledMonsterHexIds;

	internal IReadOnlySet<string> DisabledForgeIdsForPool => GetEffectiveRunConfigurationSnapshot().DisabledForgeIds;

	internal HextechRarityWeights FirstActRuneRarityWeights => GetEffectiveRunConfigurationSnapshot().FirstActRuneRarityWeights;

	internal HextechRarityWeights NormalRuneRarityWeights => GetEffectiveRunConfigurationSnapshot().NormalRuneRarityWeights;

	internal HextechRarityWeights SecondActAfterSilverRuneRarityWeights => GetEffectiveRunConfigurationSnapshot().SecondActAfterSilverRuneRarityWeights;

	internal HextechForgeRarityWeights ForgeRarityWeights => GetEffectiveRunConfigurationSnapshot().ForgeRarityWeights;

	internal int RandomForgeShopPrice => GetEffectiveRunConfigurationSnapshot().RandomForgeShopPrice;

	internal bool RandomForgeDirectGrant => GetEffectiveRunConfigurationSnapshot().RandomForgeDirectGrant;

	internal int PlayerRuneRerollLimit => GetEffectiveRunConfigurationSnapshot().PlayerRuneRerollLimit;

	internal int MonsterHexRerollLimit => GetEffectiveRunConfigurationSnapshot().MonsterHexRerollLimit;

	internal int GetPlayerHexCountForAct(int actIndex)
	{
		return _runContext.PlayerHexCounts.GetForAct(actIndex, IsEndlessLoopActive);
	}

	internal HextechRunConfigurationSnapshot GetEffectiveRunConfigurationSnapshot()
	{
		HextechRunConfigurationSnapshot? snapshot = _runContext.RunConfigurationSnapshot;
		if (snapshot != null)
		{
			return HextechRuneConfiguration.NormalizeSnapshot(snapshot with
			{
				PlayerHexCountsByAct = _runContext.PlayerHexCounts.Snapshot,
				EnemyHexCountsByAct = _runContext.EnemyHexCounts.Snapshot,
				DisabledPlayerRuneIds = PlayerRuneConfigDisabledIds.ToHashSet(StringComparer.Ordinal)
			});
		}

		bool isClient = false;
		try
		{
			isClient = RunManager.Instance.NetService.Type == NetGameType.Client;
		}
		catch
		{
			// Fall back to local configuration outside a fully initialized multiplayer run.
		}

		HextechRunConfigurationSnapshot local = isClient
			? HextechRuneConfiguration.GetDefaultSnapshot()
			: HextechRuneConfiguration.GetSnapshot();
		return HextechRuneConfiguration.NormalizeSnapshot(local with
		{
			PlayerHexCountsByAct = _runContext.PlayerHexCounts.Snapshot,
			EnemyHexCountsByAct = _runContext.EnemyHexCounts.Snapshot,
			DisabledPlayerRuneIds = PlayerRuneConfigDisabledIds.ToHashSet(StringComparer.Ordinal)
		});
	}

	internal void InitializeRunConfigurationSnapshotForNewRun(string reason)
	{
		SetRunConfigurationSnapshot(CreateNewRunConfigurationSnapshot(), reason);
	}

	internal void SetRunConfigurationSnapshot(HextechRunConfigurationSnapshot snapshot, string reason)
	{
		HextechRunConfigurationSnapshot normalized = HextechRuneConfiguration.NormalizeSnapshot(snapshot);
		_runContext.RunConfigurationSnapshot = normalized.Copy();
		_runContext.PlayerHexCounts.Set(normalized.PlayerHexCountsByAct);
		_runContext.EnemyHexCounts.Set(normalized.EnemyHexCountsByAct);
		_runContext.PlayerRuneConfig.Set(normalized.DisabledPlayerRuneIds);
		HextechLog.Info($"[{ModInfo.Id}][Mayhem] Run config snapshot set: reason={reason} playerCounts={string.Join(",", PlayerHexCountsByAct)} enemyCounts={string.Join(",", EnemyHexCountsByAct)} playerRerolls={normalized.PlayerRuneRerollLimit} monsterRerolls={normalized.MonsterHexRerollLimit} playerDisabled={PlayerRuneConfigDisabledIds.Count} enemyDisabled={normalized.DisabledMonsterHexIds.Count} forgeDisabled={normalized.DisabledForgeIds.Count} forgePrice={normalized.RandomForgeShopPrice} forgeDirect={normalized.RandomForgeDirectGrant}");
	}

	private static HextechRunConfigurationSnapshot CreateNewRunConfigurationSnapshot()
	{
		try
		{
			return RunManager.Instance.NetService.Type == NetGameType.Client
				? HextechRuneConfiguration.GetDefaultSnapshot()
				: HextechRuneConfiguration.GetSnapshot();
		}
		catch
		{
			return HextechRuneConfiguration.GetDefaultSnapshot();
		}
	}

	private string SerializeRunConfigurationSnapshot()
	{
		HextechRunConfigurationSnapshot? snapshot = _runContext.RunConfigurationSnapshot;
		return snapshot == null
			? ""
			: JsonSerializer.Serialize(HextechRuneConfiguration.NormalizeSnapshot(snapshot), HextechTelemetry.JsonOptions);
	}

	private void RestoreRunConfigurationSnapshot(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			_runContext.RunConfigurationSnapshot = null;
			return;
		}

		try
		{
			HextechRunConfigurationSnapshot? snapshot = JsonSerializer.Deserialize<HextechRunConfigurationSnapshot>(json, HextechTelemetry.JsonOptions);
			if (snapshot != null)
			{
				SetRunConfigurationSnapshot(snapshot, "restore saved run config");
			}
		}
		catch (Exception ex)
		{
			_runContext.RunConfigurationSnapshot = null;
			Log.Warn($"[{ModInfo.Id}][Mayhem] Run config snapshot restore failed; using runtime fallback: {ex.Message}", 2);
		}
	}
}
