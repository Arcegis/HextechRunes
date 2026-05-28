namespace HextechRunes;

internal sealed class AncientWineEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.AncientWine;

	internal override async Task AfterCardPlayed(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!cardPlay.IsFirstInSeries
			|| cardPlay.IsAutoPlay
			|| !IllusoryWeaponRune.IsSkillForEffects(cardPlay.Card)
			|| cardPlay.Card.Owner?.Creature.Side != CombatSide.Player
			|| cardPlay.Card.Owner.Creature.CombatState is not HextechCombatState combatState
			|| combatState.RunState != context.RunState)
		{
			return;
		}

		int heal = context.TierValue(Kind, 1, 1, 2);
		foreach (Creature enemy in context.GetAliveEnemies(combatState))
		{
			await CreatureCmd.Heal(enemy, heal);
		}
	}
}
