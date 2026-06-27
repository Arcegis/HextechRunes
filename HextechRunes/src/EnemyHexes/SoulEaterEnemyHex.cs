namespace HextechRunes;

internal sealed class SoulEaterEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.SoulEater;

	internal override async Task AfterDeath(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, Creature target, HextechCombatState combatState)
	{
		// 任意敌人死亡时,场上其它存活敌人各回复其 10% 最大生命值。
		if (target.Side != CombatSide.Enemy)
		{
			return;
		}

		foreach (Creature enemy in context.GetAliveEnemies(combatState))
		{
			if (enemy == target || !enemy.IsAlive)
			{
				continue;
			}

			int heal = Math.Max(1, (int)Math.Floor(enemy.MaxHp * 0.10m));
			await CreatureCmd.Heal(enemy, heal);
		}
	}
}
