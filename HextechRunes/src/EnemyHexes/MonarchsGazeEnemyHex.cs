namespace HextechRunes;

internal sealed class MonarchsGazeEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.MonarchsGaze;

	internal override Task AfterCardPlayed(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!cardPlay.IsFirstInSeries
			|| cardPlay.IsAutoPlay
			|| !IllusoryWeaponRune.IsAttackForEffects(cardPlay.Card, cardPlay.Card.Owner)
			|| cardPlay.Card.Owner?.Creature.Side != CombatSide.Player
			|| cardPlay.Card.Owner.Creature.CombatState?.RunState != context.RunState
			|| !cardPlay.Card.Owner.Creature.IsAlive)
		{
			return Task.CompletedTask;
		}

		return PowerCmd.Apply<HextechTemporaryStrengthLossPower>(cardPlay.Card.Owner.Creature, 1m, cardPlay.Card.Owner.Creature, cardPlay.Card);
	}
}
