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

		if (IllusoryWeaponRune.IsSkillForEffects(cardPlay.Card))
		{
			await PowerCmd.Apply<HextechTemporaryStrengthLossPower>(playerCreature, 1m, playerCreature, cardPlay.Card);
		}
		if (IllusoryWeaponRune.IsAttackForEffects(cardPlay.Card, cardPlay.Card.Owner))
		{
			await PowerCmd.Apply<HextechTemporaryDexterityLossPower>(playerCreature, 1m, playerCreature, cardPlay.Card);
		}
	}
}
