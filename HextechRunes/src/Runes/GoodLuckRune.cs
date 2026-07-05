using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace HextechRunes;

public sealed class GoodLuckRune : HextechRelicBase
{
	public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
		if (player != Owner
			|| creationOptions.Source != CardCreationSource.Encounter
			|| cardRewardOptions.Count == 0)
		{
			return false;
		}

		HashSet<ModelId> existingIds = cardRewardOptions
			.Select(static result => result.Card.CanonicalInstance.Id)
			.ToHashSet();
		List<CardModel> rarePool = creationOptions
			.GetPossibleCards(player)
			.Where(card => card.Rarity == CardRarity.Rare && !existingIds.Contains(card.Id))
			.ToList();
		if (rarePool.Count == 0)
		{
			return false;
		}

		CardCreationOptions rareOptions = HextechGameApiCompat.CreateOptionsFromCards(
				player,
				rarePool,
				creationOptions.Source,
				CardRarityOddsType.Uniform)
			.WithFlags(CardCreationFlags.NoModifyHooks);
		CardCreationResult? rareResult = CardFactory.CreateForReward(player, 1, rareOptions).FirstOrDefault();
		if (rareResult == null)
		{
			return false;
		}

		CardCmd.Upgrade(rareResult.Card, CardPreviewStyle.None);
		cardRewardOptions.Add(rareResult);
		Flash();
		return true;
	}
}
