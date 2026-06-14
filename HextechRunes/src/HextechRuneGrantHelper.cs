using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

internal static class HextechRuneGrantHelper
{
	private static readonly IReadOnlySet<Type> ExcludedRewardRuneTypes = new HashSet<Type>
	{
		typeof(TransmuteChaosRune),
		typeof(TransmutePrismaticRune),
		typeof(TransmuteGoldRune)
	};

	public static async Task ObtainRandomRunes(Player player, IEnumerable<Type> candidateTypes, int count)
	{
		await ObtainRandomRunes(player, candidateTypes, count, blockedIds: null);
	}

	public static async Task ObtainRandomRunes(Player player, IEnumerable<Type> candidateTypes, int count, IReadOnlySet<ModelId>? blockedIds)
	{
		IReadOnlyList<Type> candidates = candidateTypes as IReadOnlyList<Type> ?? candidateTypes.ToArray();
		if (await TryObtainRandomRunesMultiplayer(player, candidates, count, blockedIds))
		{
			return;
		}

		List<ModelId> selectedIds = SelectRandomRuneIds(player, candidates, count, blockedIds);
		await ObtainRuneIds(player, selectedIds);
	}

	private static async Task<bool> TryObtainRandomRunesMultiplayer(
		Player player,
		IReadOnlyList<Type> candidateTypes,
		int count,
		IReadOnlySet<ModelId>? blockedIds)
	{
		RunManager runManager = RunManager.Instance;
		NetGameType gameType = runManager.NetService.Type;
		if (gameType is not (NetGameType.Host or NetGameType.Client) || player.NetId == 0UL)
		{
			return false;
		}

		PlayerChoiceSynchronizer? synchronizer = await HextechRuneSelectionCoordinator.WaitForPlayerChoiceSynchronizerAsync(runManager);
		if (synchronizer == null)
		{
			return false;
		}

		uint choiceId = synchronizer.ReserveChoiceId(player);
		RunState runState = (RunState)player.RunState;
		if (HextechRuneSelectionCoordinator.IsLocalPlayer(runManager, player))
		{
			List<ModelId> selectedIds = SelectRandomRuneIds(player, candidateTypes, count, blockedIds);
			synchronizer.SyncLocalChoice(player, choiceId, HextechChoiceCodec.CreateRandomRuneGrant(selectedIds));
			Log.Info($"[{ModInfo.Id}][Mayhem] RandomRuneGrant sync local: player={player.NetId} choiceId={choiceId} ids={string.Join(",", selectedIds.Select(static id => id.Entry))}");
			await ObtainRuneIds(player, selectedIds);
			return true;
		}

		(PlayerChoiceResult remoteChoice, uint receivedChoiceId) = await HextechRuneSelectionCoordinator.WaitForRemoteHextechChoice(
			synchronizer,
			runState,
			player,
			choiceId,
			HextechChoiceCodec.IsRandomRuneGrant,
			"random-rune-grant");
		if (!HextechChoiceCodec.TryDecodeRandomRuneGrant(remoteChoice, out List<ModelId> syncedIds))
		{
			Log.Warn($"[{ModInfo.Id}][Mayhem] RandomRuneGrant malformed remote payload: player={player.NetId} choiceId={receivedChoiceId}");
			return false;
		}

		Log.Info($"[{ModInfo.Id}][Mayhem] RandomRuneGrant remote received: player={player.NetId} choiceId={receivedChoiceId} ids={string.Join(",", syncedIds.Select(static id => id.Entry))}");
		await ObtainRuneIds(player, syncedIds);
		return true;
	}

	private static List<ModelId> SelectRandomRuneIds(
		Player player,
		IReadOnlyList<Type> candidateTypes,
		int count,
		IReadOnlySet<ModelId>? blockedIds)
	{
		List<ModelId> selectedIds = [];
		HashSet<ModelId> selectedIdSet = [];
		for (int i = 0; i < count; i++)
		{
			List<Type> pool = BuildObtainableRunePool(player, candidateTypes, blockedIds, selectedIdSet);
			if (pool.Count == 0)
			{
				break;
			}

			Type runeType = HextechStableRandom.Pick(
				pool,
				(RunState)player.RunState,
				HextechStableRandom.TypeModelKey,
				"rune-grant",
				HextechStableRandom.PlayerKey(player),
				i.ToString(),
				count.ToString(),
				string.Join(",", selectedIdSet.Select(static id => id.Entry).OrderBy(static entry => entry, StringComparer.Ordinal)),
				blockedIds == null ? "" : string.Join(",", blockedIds.Select(static id => id.Entry).OrderBy(static entry => entry, StringComparer.Ordinal)));
			ModelId runeId = ModelDb.GetId(runeType);
			selectedIds.Add(runeId);
			selectedIdSet.Add(runeId);
		}

		return selectedIds;
	}

	private static async Task ObtainRuneIds(Player player, IEnumerable<ModelId> runeIds)
	{
		foreach (ModelId runeId in runeIds)
		{
			RelicModel relic = ModelDb.GetById<RelicModel>(runeId).ToMutable();
			SaveManager.Instance.MarkRelicAsSeen(relic);
			await RelicCmd.Obtain(relic, player);
		}
	}

	private static List<Type> BuildObtainableRunePool(
		Player player,
		IEnumerable<Type> candidateTypes,
		IReadOnlySet<ModelId>? blockedIds,
		IReadOnlySet<ModelId> selectedIds)
	{
		bool applyConfiguration = HextechRunePoolBuilder.ShouldApplyPlayerRuneConfiguration(player);
		HashSet<ModelId> ownedAndSelectedIds = player.Relics
			.Where(HextechCatalog.IsHextechRelic)
			.Select(static relic => relic.CanonicalInstance?.Id ?? relic.Id)
			.Concat(selectedIds)
			.ToHashSet();
		HashSet<ModelId> unavailableIds = ownedAndSelectedIds.ToHashSet();
		unavailableIds.UnionWith(HextechCatalog.GetMutuallyExclusivePlayerRuneIds(ownedAndSelectedIds));
		if (blockedIds != null)
		{
			unavailableIds.UnionWith(blockedIds);
		}

		return candidateTypes
			.Where(type => applyConfiguration
				? HextechCatalog.IsPlayerRuneTypeConfigurable(type)
				: HextechCatalog.IsPlayerRuneTypeSelectable(type))
			.Where(type => !applyConfiguration || HextechRuneConfiguration.IsPlayerRuneEnabled(ModelDb.GetId(type).Entry))
			.Where(type => !ExcludedRewardRuneTypes.Contains(type))
			.Where(type => !unavailableIds.Contains(ModelDb.GetId(type)))
			.Where(type =>
			{
				RelicModel relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(type));
				return HextechCatalog.IsAvailableForPlayer(relic, player);
			})
			.ToList();
	}

	public static async Task ReplaceOwnedHextechRunesWithRandomRunes(Player player, IEnumerable<Type> candidateTypes, IReadOnlySet<ModelId>? blockedIds = null)
	{
		List<RelicModel> ownedRunes = player.Relics.Where(HextechCatalog.IsHextechRelic).ToList();
		if (ownedRunes.Count == 0)
		{
			return;
		}

		foreach (RelicModel relic in ownedRunes)
		{
			await RelicCmd.Remove(relic);
		}

		await ObtainRandomRunes(player, candidateTypes, ownedRunes.Count, blockedIds);
	}

	public static async Task ConsumeAndObtainRandomRunes(RelicModel consumedRune, Player player, IEnumerable<Type> candidateTypes, int count)
	{
		await RelicCmd.Remove(consumedRune);
		await ObtainRandomRunes(player, candidateTypes, count);
	}
}
