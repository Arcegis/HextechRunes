using System.Reflection;
using HarmonyLib;
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
