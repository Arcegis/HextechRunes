namespace HextechRunes;

internal sealed class AncientWineEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.AncientWine;

	internal override async Task AfterCardPlayed(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!context.IsManualPlayerCardPlay(cardPlay, out _, out HextechCombatState? combatState)
			|| !IllusoryWeaponRune.IsSkillForEffects(cardPlay.Card))
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
