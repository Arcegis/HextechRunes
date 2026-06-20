using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using static HextechRunes.HextechHookReflection;

namespace HextechRunes;

internal static class HextechRewardSafetyHooks
{
	public static void Install(Harmony harmony)
	{
		harmony.Patch(
			RequireMethod(typeof(Reward), nameof(Reward.FromSerializable), BindingFlags.Public | BindingFlags.Static, typeof(SerializableReward), typeof(Player)),
			postfix: new HarmonyMethod(typeof(HextechRewardSafetyHooks), nameof(RewardFromSerializablePostfix)));
		harmony.Patch(
			RequireMethod(typeof(CardReward), "OnSelect", BindingFlags.Instance | BindingFlags.NonPublic),
			postfix: new HarmonyMethod(typeof(HextechRewardSafetyHooks), nameof(CardRewardOnSelectPostfix)));
	}

	private static void CardRewardOnSelectPostfix(CardReward __instance, ref Task<bool> __result)
	{
		if (!ShouldApplyForbiddenGrimoire(__instance))
		{
			return;
		}

		__result = CompleteForbiddenGrimoireCardRewardAsync(__instance, __result);
	}

	private static async Task<bool> CompleteForbiddenGrimoireCardRewardAsync(CardReward reward, Task<bool> originalTask)
	{
		bool rewardComplete = await originalTask;
		if (!rewardComplete || !ShouldApplyForbiddenGrimoire(reward))
		{
			return rewardComplete;
		}

		List<CardModel> remainingCards = reward.Cards.ToList();
		if (remainingCards.Count == 0)
		{
			return rewardComplete;
		}

		foreach (CardModel card in remainingCards)
		{
			CardPileAddResult result = await CardPileCmd.Add(card, PileType.Deck);
			if (result.success)
			{
				Log.Info($"[{ModInfo.Id}][EnemyForbiddenGrimoire] Forced unpicked card reward: player={reward.Player.NetId} card={result.cardAdded.Id.Entry}");
			}
			else
			{
				Log.Warn($"[{ModInfo.Id}][EnemyForbiddenGrimoire] Failed to force unpicked card reward: player={reward.Player.NetId} card={card.Id.Entry}", 2);
			}
		}

		return rewardComplete;
	}

	private static bool ShouldApplyForbiddenGrimoire(CardReward reward)
	{
		Player player = reward.Player;
		return player.RunState is RunState runState
			&& !player.Creature.IsDead
			&& runState.Modifiers.OfType<HextechMayhemModifier>().LastOrDefault() is HextechMayhemModifier modifier
			&& modifier.HasActiveMonsterHex(MonsterHexKind.ForbiddenGrimoire);
	}

	private static void RewardFromSerializablePostfix(SerializableReward save, Player player, ref Reward __result)
	{
		if (save.RewardType == RewardType.Gold && save.GoldAmount < 0 && __result is GoldReward)
		{
			__result = new GoldReward(0, player, save.WasGoldStolenBack);
			Log.Warn($"[{ModInfo.Id}][Rewards] Repaired serialized gold reward with negative amount {save.GoldAmount}; defaulting to 0 gold.");
			return;
		}

		if (save.RewardType == RewardType.Relic
			&& save.WasGoldStolenBack
			&& save.PredeterminedModelId != ModelId.none)
		{
			RelicModel relic = ModelDb.GetById<RelicModel>(save.PredeterminedModelId).ToMutable();
			__result = new HextechWaxRelicReward(relic, player);
			return;
		}

		if (save.RewardType == RewardType.Card
			&& save.CustomDescriptionEncounterSourceId == ModelDb.GetId<GenesisRune>())
		{
			CardCreationOptions options = new(
				save.CardPoolIds.Select(ModelDb.GetById<CardPoolModel>),
				save.Source,
				save.RarityOdds);
			__result = new GenesisUpgradedCardReward(options, save.OptionCount, player);
			return;
		}

		if (save.RewardType == RewardType.Card
			&& save.CustomDescriptionEncounterSourceId == ModelDb.GetId<ColorDiscoveryRune>()
			&& save.PredeterminedModelId != ModelId.none)
		{
			__result = ColorDiscoveryCardReward.FromSavedReward(save, player);
			return;
		}

		if (save.RewardType == RewardType.SpecialCard
			&& save.PredeterminedModelId == ModelDb.GetId<ColorDiscoveryRune>()
			&& save.SpecialCard != null)
		{
			__result = ColorDiscoveryCardReward.FromSavedSpecialCardReward(save, __result, player);
			return;
		}

		if (save.CustomDescriptionEncounterSourceId == ModelDb.GetId<RandomForgeShopRelic>()
			&& save.CardPoolIds.Count > 0)
		{
			__result = HextechForgeChoiceReward.FromSavedReward(save, player);
		}
	}
}
