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

		int maxTriggers = context.TierValue(Kind, 2, 3, 3);
		int triggers = context.Tracking.EnemyPorcupineTriggersThisTurn.GetValueOrDefault(combatId, 0);
		if (triggers >= maxTriggers)
		{
			return;
		}

		context.Tracking.EnemyPorcupineTriggersThisTurn[combatId] = triggers + 1;
		int thorns = context.TierValue(Kind, 1, 1, 2);
		context.Tracking.EnemyPorcupineTemporaryThornsThisTurn[combatId] =
			context.Tracking.EnemyPorcupineTemporaryThornsThisTurn.GetValueOrDefault(combatId, 0) + thorns;
		await PowerCmd.Apply<ThornsPower>(target, thorns, target, cardSource);
	}

	internal override async Task BeforeTurnEnd(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CombatSide side, CombatRoom? combatRoom)
	{
		if (combatRoom == null)
		{
			context.Tracking.EnemyPorcupineTriggersThisTurn.Clear();
			return;
		}

		if (context.Tracking.EnemyPorcupineTemporaryThornsThisTurn.Count > 0)
		{
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
		}

		context.Tracking.EnemyPorcupineTemporaryThornsThisTurn.Clear();
		context.Tracking.EnemyPorcupineTriggersThisTurn.Clear();
	}
}
