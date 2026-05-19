namespace HextechRunes;

internal sealed class RepulsorEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Repulsor;

	internal override async Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		foreach (uint combatId in context.Tracking.RepulsorPending.ToList())
		{
			Creature? creature = combatState.GetCreature(combatId);
			context.Tracking.RepulsorPending.Remove(combatId);
			if (creature == null || !creature.IsAlive)
			{
				continue;
			}

			await HextechEnemyPowerScalingHooks.Apply<SlipperyPower>(creature, context.TierValue(Kind, 1, 2, 3), creature, null);
		}
	}

	internal override Task AfterEnemyHealthThreshold(HextechEnemyHexContext context, Creature target, uint combatId)
	{
		if (context.Tracking.RepulsorTriggered.Add(combatId))
		{
			context.Tracking.RepulsorPending.Add(combatId);
		}

		return Task.CompletedTask;
	}
}
