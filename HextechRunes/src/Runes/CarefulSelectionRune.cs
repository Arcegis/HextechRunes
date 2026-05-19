using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

namespace HextechRunes;

public sealed class CarefulSelectionRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(4)
	];

	public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
	{
		if (player != Owner
			|| creationOptions.Source != CardCreationSource.Encounter
			|| cardRewardOptions.Count == 0)
		{
			return false;
		}

		int targetCount = DynamicVars.Cards.IntValue;
		int cardsToAdd = targetCount - cardRewardOptions.Count;
		if (cardsToAdd <= 0)
		{
			return false;
		}

		List<CardModel> candidates = BuildAdditionalCardPool(player, cardRewardOptions, creationOptions);
		if (candidates.Count == 0)
		{
			return false;
		}

		CardCreationOptions extraOptions = new CardCreationOptions(
				candidates,
				creationOptions.Source,
				GetRarityOddsForAdditionalPool(creationOptions, candidates))
			.WithFlags(creationOptions.Flags | CardCreationFlags.NoModifyHooks);
		if (creationOptions.RngOverride != null)
		{
			extraOptions.WithRngOverride(creationOptions.RngOverride);
		}

		List<CardCreationResult> extraCards = CardFactory
			.CreateForReward(player, Math.Min(cardsToAdd, candidates.Count), extraOptions)
			.ToList();
		if (extraCards.Count == 0)
		{
			return false;
		}

		cardRewardOptions.AddRange(extraCards);
		Flash();
		return true;
	}

	private static List<CardModel> BuildAdditionalCardPool(
		Player player,
		IEnumerable<CardCreationResult> cardRewardOptions,
		CardCreationOptions creationOptions)
	{
		HashSet<ModelId> existingIds = cardRewardOptions
			.Select(static result => result.Card.CanonicalInstance.Id)
			.ToHashSet();
		return creationOptions
			.GetPossibleCards(player)
			.Where(card => !existingIds.Contains(card.Id))
			.GroupBy(static card => card.Id)
			.Select(static group => group.First())
			.ToList();
	}

	private static CardRarityOddsType GetRarityOddsForAdditionalPool(
		CardCreationOptions creationOptions,
		IReadOnlyCollection<CardModel> candidates)
	{
		CardRarity? onlyRarity = candidates
			.Select(static card => card.Rarity)
			.Distinct()
			.Count() == 1
			? candidates.First().Rarity
			: null;
		return onlyRarity.HasValue ? CardRarityOddsType.Uniform : creationOptions.RarityOdds;
	}
}
