namespace HextechRunes;

internal sealed class MountainSoulEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.MountainSoul;

	internal override Task AfterEnemyDamageReceived(HextechEnemyHexContext context, Creature target, uint combatId, DamageResult result, Creature? dealer, CardModel? cardSource)
	{
		context.Tracking.MountainSoulDamagedSinceLastTurn.Add(combatId);
		return Task.CompletedTask;
	}

	internal override async Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		foreach (Creature enemy in context.GetAliveEnemies(combatState))
		{
			if (enemy.CombatId == null)
			{
				continue;
			}

			uint combatId = enemy.CombatId.Value;
			if (context.Tracking.MountainSoulHasPreviousTurn.Contains(combatId)
				&& !context.Tracking.MountainSoulDamagedSinceLastTurn.Contains(combatId))
			{
				int block = Math.Max(1, (int)Math.Floor(enemy.MaxHp * 0.1m));
				await CreatureCmd.GainBlock(enemy, block, ValueProp.Unpowered, null);
			}

			context.Tracking.MountainSoulHasPreviousTurn.Add(combatId);
			context.Tracking.MountainSoulDamagedSinceLastTurn.Remove(combatId);
		}
	}
}
