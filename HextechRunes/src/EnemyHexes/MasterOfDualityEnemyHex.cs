namespace HextechRunes;

internal sealed class MasterOfDualityEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.MasterOfDuality;

	internal override async Task AfterCardPlayed(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner?.Creature.Side != CombatSide.Player)
		{
			return;
		}

		Creature playerCreature = cardPlay.Card.Owner.Creature;
		if (!playerCreature.IsAlive)
		{
			return;
		}

		if (cardPlay.Card.Type == CardType.Skill)
		{
			await PowerCmd.Apply<HextechTemporaryStrengthLossPower>(playerCreature, 1m, playerCreature, cardPlay.Card);
		}
		else if (cardPlay.Card.Type == CardType.Attack)
		{
			await PowerCmd.Apply<HextechTemporaryDexterityLossPower>(playerCreature, 1m, playerCreature, cardPlay.Card);
		}
	}
}
