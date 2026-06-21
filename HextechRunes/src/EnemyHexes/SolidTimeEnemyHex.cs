namespace HextechRunes;

internal sealed class SolidTimeEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.SolidTime;

	internal override Task AfterCardPlayed(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		Player? owner = cardPlay.Card.Owner;
		if (owner?.Creature.Side != CombatSide.Player
			|| owner.Creature.CombatState?.RunState != context.RunState
			|| owner.Creature.IsDead
			|| cardPlay.Card.Type != CardType.Power)
		{
			return Task.CompletedTask;
		}

		return PlayerCmd.LoseEnergy(1m, owner);
	}
}
