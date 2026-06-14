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
	private static HextechRarityTier RollRandomRarity(HextechMayhemModifier modifier, int actIndex, RunState runState)
	{
		if (actIndex == 0)
		{
			return RollWeightedRarity(runState, FirstActSilverWeight, FirstActGoldWeight, FirstActPrismaticWeight, deterministic: false, actIndex);
		}

		if (actIndex == 1 && modifier.GetRarityForAct(0) == HextechRarityTier.Silver)
		{
			return RollWeightedRarity(runState, 0, 1, 1, deterministic: false, actIndex);
		}

		return (HextechRarityTier)runState.Rng.Niche.NextInt(3);
	}

	private static HextechRarityTier RollStableRarity(HextechMayhemModifier modifier, int actIndex, RunState runState)
	{
		if (actIndex == 0)
		{
			return RollWeightedRarity(runState, FirstActSilverWeight, FirstActGoldWeight, FirstActPrismaticWeight, deterministic: true, actIndex);
		}

		if (actIndex == 1 && modifier.GetRarityForAct(0) == HextechRarityTier.Silver)
		{
			return RollWeightedRarity(runState, 0, 1, 1, deterministic: true, actIndex);
		}

		return (HextechRarityTier)HextechStableRandom.Index(runState, 3, "act-roll-rarity", actIndex.ToString());
	}

	private static async Task<(HextechRarityTier Rarity, MonsterHexKind? MonsterHex)> ResolveActRoll(RunState runState, HextechMayhemModifier modifier, int actIndex)
	{
		RunManager runManager = RunManager.Instance;
		NetGameType gameType = runManager.NetService.Type;
		bool isMultiplayer = gameType is NetGameType.Host or NetGameType.Client;

		HextechRarityTier? savedRarity = modifier.GetRarityForAct(actIndex);
		HextechRarityTier? forcedRarity = HextechCustomRunModifierHooks.GetForcedRarity(runState);
		HextechRarityTier localRarity = savedRarity
			?? forcedRarity
			?? (isMultiplayer ? RollStableRarity(modifier, actIndex, runState) : RollRandomRarity(modifier, actIndex, runState));
		modifier.SetRarityForAct(actIndex, localRarity);
		if (!savedRarity.HasValue && forcedRarity.HasValue)
		{
			Log.Info($"[{ModInfo.Id}][Mayhem] ResolveActRoll forced rarity: act={actIndex} rarity={localRarity}");
		}

		IReadOnlyList<MonsterHexKind> previousHexes = modifier.GetActiveMonsterHexesBeforeAct(actIndex);
		int newEnemyHexCount = modifier.GetEnemyHexCountForAct(actIndex);
		MonsterHexKind? savedPrimaryMonsterHex = modifier.GetMonsterHexesForAct(actIndex)
			.Where(hex => !previousHexes.Contains(hex))
			.Cast<MonsterHexKind?>()
			.FirstOrDefault();
		MonsterHexKind? localMonsterHex = newEnemyHexCount <= 0
			? null
			: savedPrimaryMonsterHex
				?? (isMultiplayer
					? ChooseStableMonsterHexForAct(modifier, localRarity, runState, actIndex, previousHexes)
					: ChooseMonsterHexForAct(modifier, localRarity, runState, previousHexes));
		Log.Info($"[{ModInfo.Id}][Mayhem] ResolveActRoll enemy count: act={actIndex} newCount={newEnemyHexCount} previous={previousHexes.Count} primary={localMonsterHex}");

		if (gameType is NetGameType.Singleplayer or NetGameType.None or NetGameType.Replay)
		{
			modifier.HostUsesBetterMultiplayerScaling = false;
			return (localRarity, localMonsterHex);
		}

		PlayerChoiceSynchronizer? synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);
		Player? authorityPlayer = GetActRollAuthorityPlayer(runManager, runState);
		if (synchronizer == null || authorityPlayer == null)
		{
			modifier.HostUsesBetterMultiplayerScaling = gameType == NetGameType.Host && HextechMultiplayerScalingCompat.IsBetterMultiplayerScalingLoaded();
			Log.Warn($"[{ModInfo.Id}][Mayhem] ResolveActRoll: falling back to local roll act={actIndex} rarity={localRarity} monsterHex={localMonsterHex} enemyCounts={string.Join(",", modifier.EnemyHexCountsByAct)} synchronizer={synchronizer != null} authority={authorityPlayer?.NetId}");
			return (localRarity, localMonsterHex);
		}

		uint choiceId = synchronizer.ReserveChoiceId(authorityPlayer);
		if (gameType == NetGameType.Host)
		{
			bool hostUsesExternalScaling = HextechMultiplayerScalingCompat.IsBetterMultiplayerScalingLoaded();
			modifier.HostUsesBetterMultiplayerScaling = hostUsesExternalScaling;
			synchronizer.SyncLocalChoice(authorityPlayer, choiceId, HextechChoiceCodec.CreateActRoll(actIndex, localRarity, localMonsterHex, hostUsesExternalScaling, modifier.EnemyHexCountsByAct));
			Log.Info($"[{ModInfo.Id}][Mayhem] ResolveActRoll host sync: act={actIndex} choiceId={choiceId} authority={authorityPlayer.NetId} rarity={localRarity} monsterHex={localMonsterHex} enemyCounts={string.Join(",", modifier.EnemyHexCountsByAct)} betterMultiplayerScaling={hostUsesExternalScaling}");
			return (localRarity, localMonsterHex);
		}

		(PlayerChoiceResult remoteChoice, uint receivedChoiceId) = await WaitForRemoteHextechChoice(
			synchronizer,
			runState,
			authorityPlayer,
			choiceId,
			result => HextechChoiceCodec.TryDecodeActRoll(result, actIndex, out _, out _, out _, out _),
			$"act-roll act={actIndex}");
		if (!HextechChoiceCodec.TryDecodeActRoll(remoteChoice, actIndex, out HextechRarityTier syncedRarity, out MonsterHexKind? syncedMonsterHex, out bool syncedHostUsesExternalScaling, out int[] syncedEnemyHexCountsByAct))
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] ResolveActRoll: malformed host payload act={actIndex}; using local rarity={localRarity} monsterHex={localMonsterHex}");
			return (localRarity, localMonsterHex);
		}

		modifier.SetRarityForAct(actIndex, syncedRarity);
		modifier.SetEnemyHexCountsByActSnapshot(syncedEnemyHexCountsByAct, $"host act-roll act={actIndex}");
		modifier.HostUsesBetterMultiplayerScaling = syncedHostUsesExternalScaling;
		Log.Info($"[{ModInfo.Id}][Mayhem] ResolveActRoll client sync: act={actIndex} choiceId={receivedChoiceId} authority={authorityPlayer.NetId} rarity={syncedRarity} monsterHex={syncedMonsterHex} enemyCounts={string.Join(",", modifier.EnemyHexCountsByAct)} betterMultiplayerScaling={syncedHostUsesExternalScaling} localRarity={localRarity} localMonsterHex={localMonsterHex}");
		return (syncedRarity, syncedMonsterHex);
	}

	private static Player? GetActRollAuthorityPlayer(RunManager runManager, RunState runState)
	{
		if (runManager.NetService.Type == NetGameType.Host)
		{
			return runState.Players.FirstOrDefault(player => player.NetId == runManager.NetService.NetId);
		}

		return runState.Players.FirstOrDefault();
	}

	private static HextechRarityTier RollWeightedRarity(RunState runState, int silverWeight, int goldWeight, int prismaticWeight, bool deterministic, int actIndex)
	{
		int totalWeight = silverWeight + goldWeight + prismaticWeight;
		int roll = deterministic
			? HextechStableRandom.Index(runState, totalWeight, "act-roll-weighted-rarity", actIndex.ToString(), silverWeight.ToString(), goldWeight.ToString(), prismaticWeight.ToString())
			: runState.Rng.Niche.NextInt(totalWeight);
		if (roll < silverWeight)
		{
			return HextechRarityTier.Silver;
		}

		roll -= silverWeight;
		if (roll < goldWeight)
		{
			return HextechRarityTier.Gold;
		}

		return HextechRarityTier.Prismatic;
	}

	private static MonsterHexKind? ChooseMonsterHexForAct(HextechMayhemModifier modifier, HextechRarityTier rarity, RunState runState, IEnumerable<MonsterHexKind>? extraExcludedHexes = null)
	{
		List<MonsterHexKind> pool = BuildMonsterHexPoolForAct(modifier, rarity, extraExcludedHexes);
		return pool.Count > 0 ? pool[runState.Rng.Niche.NextInt(pool.Count)] : null;
	}

	private static MonsterHexKind? ChooseStableMonsterHexForAct(HextechMayhemModifier modifier, HextechRarityTier rarity, RunState runState, int actIndex, IEnumerable<MonsterHexKind>? extraExcludedHexes = null, int ordinal = 0)
	{
		List<MonsterHexKind> pool = BuildMonsterHexPoolForAct(modifier, rarity, extraExcludedHexes);
		if (pool.Count == 0)
		{
			return null;
		}

		return pool[HextechStableRandom.Index(
			runState,
			pool.Count,
			"act-roll-monster-hex",
			actIndex.ToString(),
			ordinal.ToString(),
			((int)rarity).ToString(),
			string.Join(",", pool.Select(static kind => ((int)kind).ToString()).OrderBy(static key => key, StringComparer.Ordinal)))];
	}

	private static List<MonsterHexKind> BuildMonsterHexPoolForAct(HextechMayhemModifier modifier, HextechRarityTier rarity, IEnumerable<MonsterHexKind>? extraExcludedHexes = null)
	{
		HashSet<MonsterHexKind> alreadyChosen = modifier.GetKnownMonsterHexes().ToHashSet();
		if (extraExcludedHexes != null)
		{
			alreadyChosen.UnionWith(extraExcludedHexes);
		}

		List<MonsterHexKind> pool = MonsterHexCatalog.GetMonsterHexesForRarity(rarity)
			.Where(kind => !alreadyChosen.Contains(kind))
			.ToList();
		if (pool.Count == 0)
		{
			pool = MonsterHexCatalog.GetMonsterHexesForRarity(rarity).ToList();
		}

		return pool;
	}

	private static IReadOnlyList<MonsterHexKind> ResolveNewMonsterHexesForAct(
		HextechMayhemModifier modifier,
		HextechRarityTier rarity,
		RunState runState,
		int actIndex,
		MonsterHexKind? primaryMonsterHex)
	{
		int newEnemyHexCount = modifier.GetEnemyHexCountForAct(actIndex);
		List<MonsterHexKind> resolvedNewHexes = [];
		HashSet<MonsterHexKind> seen = [];
		foreach (MonsterHexKind hex in modifier.GetActiveMonsterHexesBeforeAct(actIndex))
		{
			seen.Add(hex);
		}

		int addedThisAct = 0;
		if (primaryMonsterHex.HasValue
			&& addedThisAct < newEnemyHexCount
			&& seen.Add(primaryMonsterHex.Value))
		{
			resolvedNewHexes.Add(primaryMonsterHex.Value);
			addedThisAct++;
		}

		NetGameType gameType = RunManager.Instance.NetService.Type;
		bool isMultiplayer = gameType is NetGameType.Host or NetGameType.Client;
		for (int ordinal = 0; addedThisAct < newEnemyHexCount; ordinal++)
		{
			MonsterHexKind? extraHex = isMultiplayer
				? ChooseStableMonsterHexForAct(modifier, rarity, runState, actIndex, seen, ordinal + 1)
				: ChooseMonsterHexForAct(modifier, rarity, runState, seen);
			if (!extraHex.HasValue || !seen.Add(extraHex.Value))
			{
				break;
			}

			resolvedNewHexes.Add(extraHex.Value);
			addedThisAct++;
		}

		Log.Info($"[{ModInfo.Id}][Mayhem] ResolveNewMonsterHexesForAct: act={actIndex} newCount={newEnemyHexCount} previous={seen.Count - addedThisAct} primary={primaryMonsterHex} newHexes={string.Join(",", resolvedNewHexes)}");
		return resolvedNewHexes;
	}

	private static IReadOnlyList<MonsterHexKind> CombineMonsterHexes(IEnumerable<MonsterHexKind> previousHexes, IEnumerable<MonsterHexKind> newHexes)
	{
		List<MonsterHexKind> combined = [];
		HashSet<MonsterHexKind> seen = [];
		foreach (MonsterHexKind hex in previousHexes)
		{
			if (seen.Add(hex))
			{
				combined.Add(hex);
			}
		}

		foreach (MonsterHexKind hex in newHexes)
		{
			if (seen.Add(hex))
			{
				combined.Add(hex);
			}
		}

		return combined;
	}

	private static MonsterHexKind? RerollEnemyHexForAct(
		HextechMayhemModifier modifier,
		HextechRarityTier rarity,
		RunState runState,
		int actIndex,
		MonsterHexKind? currentHex,
		int rerollOrdinal,
		IReadOnlySet<ModelId> excludedIconRelicIds)
	{
		HashSet<MonsterHexKind> alreadyChosen = modifier.GetKnownMonsterHexes()
			.Where(kind => kind != currentHex)
			.ToHashSet();
		List<MonsterHexKind> pool = MonsterHexCatalog.GetMonsterHexesForRarity(rarity)
			.Where(kind => kind != currentHex)
			.Where(kind => !alreadyChosen.Contains(kind))
			.Where(kind => !excludedIconRelicIds.Contains(GetMonsterHexIconRelicId(kind)))
			.ToList();
		if (pool.Count == 0)
		{
			pool = MonsterHexCatalog.GetMonsterHexesForRarity(rarity)
				.Where(kind => kind != currentHex)
				.Where(kind => !alreadyChosen.Contains(kind))
				.ToList();
		}
		if (pool.Count == 0)
		{
			pool = MonsterHexCatalog.GetMonsterHexesForRarity(rarity)
				.Where(kind => kind != currentHex)
				.ToList();
		}
		if (pool.Count == 0)
		{
			pool = MonsterHexCatalog.GetMonsterHexesForRarity(rarity).ToList();
		}
		if (pool.Count == 0)
		{
			return currentHex;
		}

		string poolKey = string.Join(",", pool.Select(static kind => ((int)kind).ToString()).OrderBy(static key => key, StringComparer.Ordinal));
		int index = HextechStableRandom.Index(
			runState,
			pool.Count,
			"enemy-hex-reroll",
			actIndex.ToString(),
			((int)rarity).ToString(),
			(currentHex.HasValue ? ((int)currentHex.Value).ToString() : "none"),
			rerollOrdinal.ToString(),
			poolKey);
		return pool[index];
	}

	private static ModelId GetMonsterHexIconRelicId(MonsterHexKind hex)
	{
		RelicModel relic = MonsterHexCatalog.GetIconRelicForMonsterHex(hex);
		return relic.CanonicalInstance?.Id ?? relic.Id;
	}
}
