namespace HextechRunes;

internal sealed class BlankCheckEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.BlankCheck;

	// 打出卡牌后，该卡牌会在本场战斗中被添加[虚无]词条。
	internal override Task AfterCardPlayed(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		CardModel card = cardPlay.Card;
		if (!cardPlay.IsFirstInSeries
			|| cardPlay.IsAutoPlay
			|| card.Owner?.Creature.Side != CombatSide.Player
			|| card.Owner.Creature.CombatState?.RunState != context.RunState
			|| card.Keywords.Contains(CardKeyword.Ethereal))
		{
			return Task.CompletedTask;
		}

		CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
		return Task.CompletedTask;
	}
}
