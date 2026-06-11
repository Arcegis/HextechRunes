namespace HextechRunes;

internal sealed class PorcupineEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Porcupine;

	internal override async Task AfterEnemyDamageReceived(HextechEnemyHexContext context, Creature target, uint combatId, DamageResult result, Creature? dealer, CardModel? cardSource)
	{
		if (!target.IsAlive || result.UnblockedDamage <= 0m)
		{
			return;
		}

		int thorns = context.TierValue(Kind, 1, 2, 3);
		context.Tracking.EnemyPorcupineTemporaryThornsThisTurn[combatId] =
			context.Tracking.EnemyPorcupineTemporaryThornsThisTurn.GetValueOrDefault(combatId, 0) + thorns;
		await PowerCmd.Apply<ThornsPower>(target, thorns, target, cardSource);
	}

	internal override async Task BeforeTurnEnd(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CombatSide side, CombatRoom? combatRoom)
	{
		if (combatRoom == null || context.Tracking.EnemyPorcupineTemporaryThornsThisTurn.Count == 0)
		{
			return;
		}

		foreach ((uint combatId, int thorns) in context.Tracking.EnemyPorcupineTemporaryThornsThisTurn.ToArray())
		{
			if (thorns <= 0)
			{
				continue;
			}

			Creature? enemy = combatRoom.CombatState.Enemies.FirstOrDefault(creature => creature.CombatId == combatId);
			if (enemy is { IsAlive: true })
			{
				await PowerCmd.Apply<ThornsPower>(enemy, -thorns, enemy, null);
			}
		}

		context.Tracking.EnemyPorcupineTemporaryThornsThisTurn.Clear();
	}
}
