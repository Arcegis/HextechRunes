namespace HextechRunes;

internal sealed class ForgottenSoulEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.ForgottenSoul;

	internal override bool ShouldEtherealTrigger(HextechEnemyHexContext context, CardModel card)
	{
		return !CanAffect(context, card) || !card.Keywords.Contains(CardKeyword.Ethereal);
	}

	internal override (PileType, CardPilePosition)? ModifyCardPlayResultPileTypeAndPosition(
		HextechEnemyHexContext context,
		CardModel card,
		bool isAutoPlay,
		ResourceInfo resources,
		PileType pileType,
		CardPilePosition position)
	{
		return CanAffect(context, card) && pileType == PileType.Exhaust
			? (PileType.Discard, position)
			: null;
	}

	internal static bool ShouldPreventPlayExhaust(CardModel card)
	{
		return card.Owner?.Creature.Side == CombatSide.Player
			&& card.Owner.RunState is RunState runState
			&& runState.Modifiers.OfType<HextechMayhemModifier>().LastOrDefault() is { } modifier
			&& modifier.HasActiveMonsterHex(MonsterHexKind.ForgottenSoul)
			&& CanAffect(card);
	}

	private static bool CanAffect(HextechEnemyHexContext context, CardModel card)
	{
		return card.Owner?.Creature.CombatState?.RunState == context.RunState && CanAffect(card);
	}

	private static bool CanAffect(CardModel card)
	{
		return card.Type is CardType.Status or CardType.Curse
			&& (card.Keywords.Contains(CardKeyword.Exhaust)
				|| card.Keywords.Contains(CardKeyword.Ethereal)
				|| card.ExhaustOnNextPlay);
	}
}
