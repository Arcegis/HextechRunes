namespace HextechRunes;

internal sealed class GetExcitedEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.GetExcited;

	internal override async Task BeforeSideTurnStart(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		foreach (Creature enemy in combatState.Enemies.ToList())
		{
			if (enemy.CombatState != combatState)
			{
				continue;
			}

			PainfulStabsPower? legacyPower = enemy.GetPower<PainfulStabsPower>();
			if (legacyPower != null && enemy.IsDead)
			{
				await PowerCmd.Remove(legacyPower);
			}

			HextechCombatCreatureHelper.RemoveRetainedDeadEnemyIfNeeded(combatState, enemy);
		}
	}

	internal override async Task BeforeDeath(HextechEnemyHexContext context, Creature creature)
	{
		if (creature.Side != CombatSide.Enemy || creature.CombatState?.RunState != context.RunState)
		{
			return;
		}

		PainfulStabsPower? painfulStabs = creature.GetPower<PainfulStabsPower>();
		if (painfulStabs != null)
		{
			await PowerCmd.Remove(painfulStabs);
		}
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
