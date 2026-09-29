#if !STS2_109_OR_NEWER
using MegaCrit.Sts2.Core.Hooks;

namespace HextechRunes;

/// <summary>出牌结果去向的版本差异。0.109 之前只有结果堆与位置，结果始终归出牌者本人。</summary>
internal static class HextechCardPlayResultCompat
{
	internal static async Task<HextechFormCardResult> ResolveAutoPlayResult(
		HextechCombatState combatState,
		CardModel card,
		ResourceInfo resources)
	{
		(PileType pileType, CardPilePosition position) = Hook.ModifyCardPlayResultPileTypeAndPosition(
			combatState,
			card,
			isAutoPlay: true,
			resources,
			PileType.None,
			CardPilePosition.Bottom,
			out IEnumerable<AbstractModel> modifiers);
		foreach (AbstractModel modifier in modifiers)
		{
			await modifier.AfterModifyingCardPlayResultPileOrPosition(card, pileType, position);
		}

		return new HextechFormCardResult(card.Owner, pileType, position);
	}

	internal static Task<bool> TryGiveToAnotherPlayer(CardModel card, HextechFormCardResult result)
	{
		return Task.FromResult(false);
	}
}
#endif
