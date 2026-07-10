namespace HextechRunes;

internal sealed class GetExcitedEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.GetExcited;

	internal override async Task BeforeSideTurnStart(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		await HextechCombatCreatureHelper.CleanUpRetainedPainfulStabsEnemies(combatState);
	}

	internal override async Task BeforeDeath(HextechEnemyHexContext context, Creature creature)
	{
		await HextechCombatCreatureHelper.RemovePainfulStabsBeforeDeath(context, creature);
	}

	internal override async Task AfterDeath(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, Creature target, HextechCombatState combatState)
	{
		IReadOnlyList<Creature> enemies = context.GetAliveEnemies(combatState)
			.Where(enemy => enemy != target)
			.ToList();
		if (enemies.Count > 0)
		{
			await PowerCmd.Apply<StrengthPower>(enemies, 1m, null, null);
			await PowerCmd.Apply<PainfulStabsPower>(enemies, 1m, null, null);
		}
	}
}
