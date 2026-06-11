namespace HextechRunes;

internal sealed class ArcanePunchEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.ArcanePunch;

	internal override async Task AfterCardPlayed(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!cardPlay.IsFirstInSeries
			|| cardPlay.IsAutoPlay
			|| !IllusoryWeaponRune.IsAttackForEffects(cardPlay.Card, cardPlay.Card.Owner)
			|| cardPlay.Card.Owner?.Creature.Side != CombatSide.Player
			|| cardPlay.Card.Owner.Creature.CombatState is not HextechCombatState combatState
			|| combatState.RunState != context.RunState)
		{
			return;
		}

		int threshold = context.TierValue(Kind, 2, 1, 1);
		int vigor = context.TierValue(Kind, 1, 1, 2);
		context.Tracking.ArcanePunchPlayerAttackCardsPlayed++;
		if (context.Tracking.ArcanePunchPlayerAttackCardsPlayed < threshold)
		{
			return;
		}

		context.Tracking.ArcanePunchPlayerAttackCardsPlayed = 0;
		foreach (Creature enemy in context.GetAliveEnemies(combatState))
		{
			await PowerCmd.Apply<VigorPower>(enemy, vigor, enemy, null);
		}
	}
}
