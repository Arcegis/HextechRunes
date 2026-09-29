namespace HextechRunes;

internal sealed class UpgradeEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Upgrade;

	internal override Task AfterCardPlayed(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!context.IsManualPlayerCardPlay(cardPlay, out _, out _)
			|| !cardPlay.Card.IsUpgraded)
		{
			return Task.CompletedTask;
		}

		CardCmd.Downgrade(cardPlay.Card);
		return Task.CompletedTask;
	}
}
