namespace HextechRunes;

internal sealed class MirrorReflectionEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.MirrorReflection;

	internal override async Task AfterCardPlayed(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!context.IsManualPlayerCardPlay(cardPlay, out _, out HextechCombatState? combatState)
			|| !cardPlay.Card.IsBasicStrikeOrDefend)
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
