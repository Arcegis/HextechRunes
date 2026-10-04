namespace HextechRunes;

internal sealed class TankEngineEnemyHex : HextechEnemyHexEffect, IHextechEnemyMaxHpCoefficientProvider
{
	internal const float BodyScalePerStack = 0.05f;

	internal override MonsterHexKind Kind => MonsterHexKind.TankEngine;

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		foreach (Creature enemy in enemies)
		{
			if (enemy.CombatId is not uint combatId)
			{
				continue;
			}

			int currentRound = combatState.RoundNumber;
			if (context.Tracking.TankEngineLastAppliedRound.GetValueOrDefault(combatId, 0) == currentRound)
			{
				continue;
			}

			// 先记下本回合已结算与新层数再等待重算：命令链里重入时不会重复叠层。
			context.Modifier.CaptureMonsterMaxHpCoefficientBase(enemy);
			context.Tracking.TankEngineLastAppliedRound[combatId] = currentRound;
			context.Tracking.TankEngineStacks[combatId] = context.Tracking.TankEngineStacks.GetValueOrDefault(combatId, 0) + 1;
			await context.Modifier.ReapplyMonsterMaxHpCoefficients(enemy);
			context.UpdateEnemyScale(enemy);
		}
	}

	public decimal GetMaxHpBonusFraction(HextechEnemyHexContext context, Creature creature)
	{
		int stacks = creature.CombatId is uint combatId
			? context.Tracking.TankEngineStacks.GetValueOrDefault(combatId, 0)
			: 0;
		return Math.Max(0, stacks) * 0.05m;
	}
}
