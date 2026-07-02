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

		await PowerCmd.Apply<HextechGalvanicPower>(
			owner.Creature,
			context.TierValue(Kind, 2, 4, 6),
			null,
			cardPlay.Card);
	}
}
