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
			return RollWeightedRarity(runState, FirstActSilverWeight, FirstActGoldWeight, FirstActPrismaticWeight);
		}

		if (actIndex == 1 && modifier.GetRarityForAct(0) == HextechRarityTier.Silver)
		{
			return RollWeightedRarity(runState, 0, 1, 1);
		}

		return (HextechRarityTier)runState.Rng.Niche.NextInt(3);
	}

	private static async Task<(HextechRarityTier Rarity, MonsterHexKind MonsterHex)> ResolveActRoll(RunState runState, HextechMayhemModifier modifier, int actIndex)
	{
		HextechRarityTier localRarity = modifier.GetRarityForAct(actIndex) ?? RollRandomRarity(modifier, actIndex, runState);
		modifier.SetRarityForAct(actIndex, localRarity);

		MonsterHexKind localMonsterHex = modifier.GetMonsterHexForAct(actIndex) ?? ChooseMonsterHexForAct(modifier, localRarity, runState);
		modifier.SetMonsterHexForAct(actIndex, localMonsterHex);

		RunManager runManager = RunManager.Instance;
		NetGameType gameType = runManager.NetService.Type;
		if (gameType is NetGameType.Singleplayer or NetGameType.None or NetGameType.Replay)
		{
			return (localRarity, localMonsterHex);
		}

		PlayerChoiceSynchronizer? synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);
		Player? authorityPlayer = GetActRollAuthorityPlayer(runManager, runState);
		if (synchronizer == null || authorityPlayer == null)
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] ResolveActRoll: falling back to local roll act={actIndex} rarity={localRarity} monsterHex={localMonsterHex} synchronizer={synchronizer != null} authority={authorityPlayer?.NetId}");
			return (localRarity, localMonsterHex);
		}

		uint choiceId = synchronizer.ReserveChoiceId(authorityPlayer);
		if (gameType == NetGameType.Host)
		{
			synchronizer.SyncLocalChoice(authorityPlayer, choiceId, CreateActRollChoiceResult(actIndex, localRarity, localMonsterHex));
			Log.Info($"[{ModInfo.Id}][Mayhem] ResolveActRoll host sync: act={actIndex} choiceId={choiceId} authority={authorityPlayer.NetId} rarity={localRarity} monsterHex={localMonsterHex}");
			return (localRarity, localMonsterHex);
		}

		(PlayerChoiceResult remoteChoice, uint receivedChoiceId) = await WaitForRemoteHextechChoice(
			synchronizer,
			authorityPlayer,
			choiceId,
			result => TryDecodeActRollChoiceResult(result, actIndex, out _, out _),
			$"act-roll act={actIndex}");
		if (!TryDecodeActRollChoiceResult(remoteChoice, actIndex, out HextechRarityTier syncedRarity, out MonsterHexKind syncedMonsterHex))
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] ResolveActRoll: malformed host payload act={actIndex}; using local rarity={localRarity} monsterHex={localMonsterHex}");
			return (localRarity, localMonsterHex);
		}

		modifier.SetRarityForAct(actIndex, syncedRarity);
		modifier.SetMonsterHexForAct(actIndex, syncedMonsterHex);
		Log.Info($"[{ModInfo.Id}][Mayhem] ResolveActRoll client sync: act={actIndex} choiceId={receivedChoiceId} authority={authorityPlayer.NetId} rarity={syncedRarity} monsterHex={syncedMonsterHex} localRarity={localRarity} localMonsterHex={localMonsterHex}");
		return (syncedRarity, syncedMonsterHex);
	}

	private static PlayerChoiceResult CreateActRollChoiceResult(int actIndex, HextechRarityTier rarity, MonsterHexKind monsterHex)
	{
		return PlayerChoiceResult.FromIndexes([ HextechChoiceMagic, ChoiceKindActRoll, actIndex, (int)rarity, (int)monsterHex ]);
	}

	private static bool TryDecodeActRollChoiceResult(PlayerChoiceResult result, int expectedActIndex, out HextechRarityTier rarity, out MonsterHexKind monsterHex)
	{
		rarity = default;
		monsterHex = default;
		if (!TryGetIndexPayload(result, out List<int>? payload)
			|| payload.Count < 5
			|| payload[0] != HextechChoiceMagic
			|| payload[1] != ChoiceKindActRoll
			|| payload[2] != expectedActIndex)
		{
			return false;
		}

		if (!Enum.IsDefined(typeof(HextechRarityTier), payload[3]) || !Enum.IsDefined(typeof(MonsterHexKind), payload[4]))
		{
			return false;
		}

		rarity = (HextechRarityTier)payload[3];
		monsterHex = (MonsterHexKind)payload[4];
		return true;
	}

	private static Player? GetActRollAuthorityPlayer(RunManager runManager, RunState runState)
	{
		if (runManager.NetService.Type == NetGameType.Host)
		{
			return runState.Players.FirstOrDefault(player => player.NetId == runManager.NetService.NetId);
		}

		return runState.Players.FirstOrDefault();
	}

	private static HextechRarityTier RollWeightedRarity(RunState runState, int silverWeight, int goldWeight, int prismaticWeight)
	{
		int totalWeight = silverWeight + goldWeight + prismaticWeight;
		int roll = runState.Rng.Niche.NextInt(totalWeight);
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

	private static MonsterHexKind ChooseMonsterHexForAct(HextechMayhemModifier modifier, HextechRarityTier rarity, RunState runState)
	{
		HashSet<MonsterHexKind> alreadyChosen = [];
		for (int i = 0; i < 3; i++)
		{
			MonsterHexKind? kind = modifier.GetMonsterHexForAct(i);
			if (kind.HasValue)
			{
				alreadyChosen.Add(kind.Value);
			}
		}

		List<MonsterHexKind> pool = ModInfo.GetMonsterHexesForRarity(rarity)
			.Where(kind => !alreadyChosen.Contains(kind))
			.ToList();
		if (pool.Count == 0)
		{
			pool = ModInfo.GetMonsterHexesForRarity(rarity).ToList();
		}

		return pool[runState.Rng.Niche.NextInt(pool.Count)];
	}
}
