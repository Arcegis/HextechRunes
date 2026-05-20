namespace HextechRunes;

internal sealed class UpgradeEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Upgrade;

	internal override Task AfterCardPlayed(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!cardPlay.IsFirstInSeries
			|| cardPlay.IsAutoPlay
			|| !cardPlay.Card.IsUpgraded
			|| cardPlay.Card.Owner?.Creature.Side != CombatSide.Player
			|| cardPlay.Card.Owner.Creature.CombatState?.RunState != context.RunState)
		{
			return Task.CompletedTask;
		}

		CardCmd.Downgrade(cardPlay.Card);
		return Task.CompletedTask;
	}
}
