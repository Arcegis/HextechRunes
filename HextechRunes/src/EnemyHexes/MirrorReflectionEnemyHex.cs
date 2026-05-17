namespace HextechRunes;

internal sealed class MirrorReflectionEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.MirrorReflection;

	internal override async Task AfterCardPlayed(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!cardPlay.IsFirstInSeries
			|| cardPlay.IsAutoPlay
			|| !cardPlay.Card.IsBasicStrikeOrDefend
			|| cardPlay.Card.Owner?.Creature.Side != CombatSide.Player
			|| cardPlay.Card.Owner.Creature.CombatState is not HextechCombatState combatState
			|| combatState.RunState != context.RunState)
		{
			return;
		}

		CardModel copy = combatState.CloneCard(cardPlay.Card);
		await HextechCardGeneration.AddGeneratedCardToCombat(
			copy,
			PileType.Discard,
			addedByPlayer: false,
			CardPilePosition.Top);
	}
}
