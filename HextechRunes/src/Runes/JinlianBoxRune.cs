using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace HextechRunes;

public sealed class JinlianBoxRune : HextechRelicBase
{
	private const int MinimumStrikeAndDefendCount = 4;

	public override bool HasUponPickupEffect => true;

	public override bool IsAvailableForPlayer(Player player)
	{
		return player.Deck.Cards.Count(static card => card.IsBasicStrikeOrDefend) >= MinimumStrikeAndDefendCount;
	}

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		List<CardModel> rareOptions = Owner.Character.CardPool
			.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
			.Where(static card => card.Rarity == CardRarity.Rare)
			.ToList();
		if (rareOptions.Count == 0)
		{
			return;
		}

		List<CardModel> transformableBasics = Owner.Deck.Cards
			.Where(static card => card.IsTransformable && card.IsBasicStrikeOrDefend)
			.ToList();
		List<CardTransformation> transformations = transformableBasics
			.Select((card, index) => CardTransformUpgradeHelper.CreateStableOptionTransformation(
				card,
				rareOptions,
				(RunState)Owner.RunState,
				"jinlian-box-rare-transform",
				index,
				HextechStableRandom.PlayerKey(Owner),
				HextechStableRandom.CardPileKey(transformableBasics)))
			.ToList();
		if (transformations.Count == 0)
		{
			return;
		}

		Flash();
		await CardCmd.Transform(transformations, null, CardPreviewStyle.GridLayout);
	}
}
