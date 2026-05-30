using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

public sealed class CompactUpgradeRune : CardUpgradeRuneBase<Compact>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsDefectPlayer(player);
	}

	internal static bool ShouldUseUpgradedPlay(CardModel card)
	{
		return card is Compact && card.Owner?.GetRelic<CompactUpgradeRune>() != null;
	}

	internal static async Task PlayUpgraded(PlayerChoiceContext choiceContext, Compact card, CardPlay cardPlay)
	{
		var owner = card.Owner!;
		var combatState = card.CombatState!;
		if (owner.PlayerCombatState == null)
		{
			return;
		}

		await CreatureCmd.GainBlock(owner.Creature, card.DynamicVars.Block, cardPlay);

		List<CardModel> statusCards = owner.PlayerCombatState.AllCards
			.Where(static candidate => candidate.IsTransformable && candidate.Type == CardType.Status)
			.ToList();
		if (statusCards.Count == 0)
		{
			return;
		}

		CompactUpgradeRune? rune = owner.GetRelic<CompactUpgradeRune>();
		rune?.Flash();
		foreach (CardModel statusCard in statusCards)
		{
			CardModel fuel = combatState.CreateCard<Fuel>(owner);
			if (card.IsUpgraded)
			{
				CardCmd.Upgrade(fuel);
			}

			await CardCmd.Transform(statusCard, fuel);
		}
	}
}
