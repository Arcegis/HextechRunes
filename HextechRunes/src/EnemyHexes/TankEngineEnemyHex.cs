namespace HextechRunes;

internal sealed class TankEngineEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.TankEngine;

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		foreach (Creature enemy in enemies)
		{
			int hpGain = Math.Min(5, Math.Max(1, (int)Math.Floor(enemy.MaxHp * 0.05m)));
			await CreatureCmd.GainMaxHp(enemy, hpGain);
			if (enemy.CombatId != null)
			{
				uint combatId = enemy.CombatId.Value;
				context.Tracking.TankEngineStacks[combatId] = context.Tracking.TankEngineStacks.GetValueOrDefault(combatId, 0) + 1;
				context.UpdateEnemyScale(enemy);
			}
		}
	}
}
