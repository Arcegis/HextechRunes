using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
	{
		if (!HasActiveMonsterHex(MonsterHexKind.CuttingEdgeAlchemist)
			|| room is not CombatRoom
			|| rewards.Count == 0)
		{
			return false;
		}

		bool modified = false;
		for (int i = 0; i < rewards.Count; i++)
		{
			if (rewards[i] is not PotionReward potionReward
				|| potionReward.Potion?.Rarity == PotionRarity.Common
				|| !TryCreateCommonPotionReward(player, i, out PotionReward? replacement)
				|| replacement == null)
			{
				continue;
			}

			rewards[i] = replacement;
			modified = true;
		}

		return modified;
	}

	private static bool TryCreateCommonPotionReward(Player player, int rewardIndex, out PotionReward? reward)
	{
		List<PotionModel> candidates = PotionFactory.GetPotionOptions(player, Array.Empty<PotionModel>())
			.Where(static potion => potion.Rarity == PotionRarity.Common)
			.ToList();
		if (candidates.Count == 0)
		{
			reward = null;
			return false;
		}

		PotionModel potion = HextechStableRandom.Pick(
			candidates,
			(RunState)player.RunState,
			HextechStableRandom.PotionKey,
			"enemy-cutting-edge-alchemist-common-potion",
			HextechStableRandom.PlayerKey(player),
			rewardIndex.ToString()).ToMutable();
		reward = new PotionReward(potion, player);
		return true;
	}
}
