namespace HextechRunes;

internal sealed class JinlianBoxEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.JinlianBox;

	internal override bool TryModifyCardRewardOptions(HextechEnemyHexContext context, Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
		return TryRemoveRareCardRewards(context, player, cardRewardOptions, creationOptions);
	}

	internal override bool TryModifyCardRewardOptionsLate(HextechEnemyHexContext context, Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
		return TryRemoveRareCardRewards(context, player, cardRewardOptions, creationOptions);
	}

	private static bool TryRemoveRareCardRewards(HextechEnemyHexContext context, Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
		if (player.RunState != context.RunState
			|| creationOptions.Source != CardCreationSource.Encounter
			|| cardRewardOptions.Count == 0)
		{
			return false;
		}

		HashSet<ModelId> existingIds = cardRewardOptions
			.Select(static result => result.Card.CanonicalInstance.Id)
			.ToHashSet();
		bool modified = false;
		for (int i = 0; i < cardRewardOptions.Count; i++)
		{
			if (cardRewardOptions[i].Card.Rarity != CardRarity.Rare)
			{
				continue;
			}

			if (TryCreateNonRareCardReward(player, creationOptions, existingIds, out CardCreationResult? replacement)
				&& replacement != null)
			{
				cardRewardOptions[i] = replacement;
				existingIds.Add(replacement.Card.CanonicalInstance.Id);
			}
			else
			{
				cardRewardOptions.RemoveAt(i);
				i--;
			}

			modified = true;
		}

		return modified;
	}

	private static bool TryCreateNonRareCardReward(
		Player player,
		CardCreationOptions creationOptions,
		IReadOnlySet<ModelId> existingIds,
		out CardCreationResult? reward)
	{
		List<CardModel> nonRarePool = creationOptions
			.GetPossibleCards(player)
			.Where(card => card.Rarity != CardRarity.Rare && !existingIds.Contains(card.Id))
			.ToList();
		if (nonRarePool.Count == 0)
		{
			reward = null;
			return false;
		}

		CardCreationOptions nonRareOptions = HextechGameApiCompat.CreateOptionsFromCards(
				player,
				nonRarePool,
				creationOptions.Source,
				CardRarityOddsType.Uniform)
			.WithFlags(CardCreationFlags.NoModifyHooks);
		reward = CardFactory.CreateForReward(player, 1, nonRareOptions).FirstOrDefault();
		return reward != null;
	}
}
