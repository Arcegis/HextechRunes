using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

public sealed class HiddenGemUpgradeRune : CardUpgradeRuneBase<HiddenGem>
{
	protected override bool IsAvailableForCharacter(Player player)
	{
		return true;
	}

	internal static bool ShouldUseUpgradedPlay(CardModel card)
	{
		return card is HiddenGem && card.Owner?.GetRelic<HiddenGemUpgradeRune>() != null;
	}

	internal static async Task PlayUpgraded(PlayerChoiceContext choiceContext, HiddenGem card, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);

		List<CardModel> drawCards = PileType.Draw.GetPile(card.Owner).Cards.ToList();
		if (drawCards.Count == 0)
		{
			return;
		}

		List<CardModel> candidates = drawCards
			.Where(static candidate =>
				!candidate.Keywords.Contains(CardKeyword.Unplayable)
				&& candidate.Type is not CardType.Status and not CardType.Curse)
			.ToList();
		if (candidates.Count == 0)
		{
			return;
		}

		List<CardModel> preferred = candidates
			.Where(static candidate => candidate.Type is CardType.Attack or CardType.Skill or CardType.Power)
			.ToList();
		IEnumerable<CardModel> pool = preferred.Count == 0 ? candidates : preferred;
		CardModel? selected = card.Owner.RunState.Rng.CombatCardSelection.NextItem(pool);
		if (selected == null)
		{
			return;
		}

		selected.BaseReplayCount += card.DynamicVars["Replay"].IntValue;
		card.Owner.GetRelic<HiddenGemUpgradeRune>()?.Flash();
		CardCmd.Preview(selected);
	}
}
