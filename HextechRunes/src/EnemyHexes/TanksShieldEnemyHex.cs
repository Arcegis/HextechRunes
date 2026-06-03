namespace HextechRunes;

internal sealed class TanksShieldEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.TanksShield;

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

		int block = context.TierValue(Kind, 1, 2, 3);
		foreach (Creature enemy in context.GetAliveEnemies(combatState))
		{
			await CreatureCmd.GainBlock(enemy, block, ValueProp.Unpowered, cardPlay);
		}
	}
}
