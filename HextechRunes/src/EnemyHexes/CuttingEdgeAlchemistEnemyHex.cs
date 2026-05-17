namespace HextechRunes;

internal sealed class CuttingEdgeAlchemistEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.CuttingEdgeAlchemist;

	internal override bool TryModifyRewards(HextechEnemyHexContext context, Player player, List<Reward> rewards, AbstractRoom? room)
	{
		if (room is not CombatRoom || rewards.Count == 0)
		{
			return false;
		}

		bool modified = false;
		for (int i = 0; i < rewards.Count; i++)
		{
			if (rewards[i] is not PotionReward potionReward
				|| potionReward.Potion?.Rarity == PotionRarity.Common
				|| !TryCreateCommonPotionReward(player, context.RunState, i, out PotionReward? replacement)
				|| replacement == null)
			{
				continue;
			}

			rewards[i] = replacement;
			modified = true;
		}

		return modified;
	}

	private static bool TryCreateCommonPotionReward(Player player, RunState runState, int rewardIndex, out PotionReward? reward)
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
			runState,
			HextechStableRandom.PotionKey,
			"enemy-cutting-edge-alchemist-common-potion",
			HextechStableRandom.PlayerKey(player),
			rewardIndex.ToString()).ToMutable();
		reward = new PotionReward(potion, player);
		return true;
	}
}
