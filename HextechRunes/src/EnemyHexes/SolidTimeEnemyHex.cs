namespace HextechRunes;

internal sealed class SolidTimeEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.SolidTime;

	internal override async Task AfterCardPlayed(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		Player? owner = cardPlay.Card.Owner;
		if (owner?.Creature.Side != CombatSide.Player
			|| owner.Creature.CombatState is not HextechCombatState combatState
			|| combatState.RunState != context.RunState
			|| owner.Creature.IsDead
			|| cardPlay.Card.Type != CardType.Power)
		{
			return;
		}

		List<CardModel> hand = PileType.Hand.GetPile(owner).Cards
			.Where(card => card != cardPlay.Card)
			.OrderBy(HextechStableRandom.CardKey, StringComparer.Ordinal)
			.ToList();
		if (hand.Count == 0)
		{
			return;
		}

		CardModel exhausted = HextechStableRandom.Pick(
			hand,
			(RunState)context.RunState,
			HextechStableRandom.CardKey,
			"enemy-solid-time-exhaust",
			HextechStableRandom.PlayerKey(owner),
			combatState.RoundNumber.ToString(),
			HextechStableRandom.CardPileKey(hand));
		await CardCmd.Exhaust(choiceContext, exhausted, causedByEthereal: false, skipVisuals: false);
	}
}
