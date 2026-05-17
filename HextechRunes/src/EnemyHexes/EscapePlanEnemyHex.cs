namespace HextechRunes;

internal sealed class EscapePlanEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.EscapePlan;

	internal override async Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		foreach (Creature creature in context.GetAliveEnemies(combatState))
		{
			if (creature.CombatId == null
				|| context.Tracking.EscapePlanTriggered.Contains(creature.CombatId.Value)
				|| context.Tracking.EscapePlanPending.Contains(creature.CombatId.Value)
				|| creature.CurrentHp >= creature.MaxHp * HextechMayhemModifier.EscapePlanHealthThresholdPercent)
			{
				continue;
			}

			uint combatId = creature.CombatId.Value;
			context.Tracking.EscapePlanTriggered.Add(combatId);
			context.Tracking.EscapePlanPending.Add(combatId);
		}

		foreach (uint combatId in context.Tracking.EscapePlanPending.ToList())
		{
			Creature? creature = combatState.GetCreature(combatId);
			context.Tracking.EscapePlanPending.Remove(combatId);
			if (creature == null || !creature.IsAlive)
			{
				continue;
			}

			int blockAmount = (int)Math.Floor(creature.MaxHp * HextechMayhemModifier.EscapePlanBlockPercent);
			if (blockAmount > 0)
			{
				await CreatureCmd.GainBlock(creature, blockAmount, ValueProp.Unpowered, null);
			}

			await PowerCmd.Apply<ShrinkPower>(creature, 1m, creature, null);
		}
	}

	internal override Task AfterEnemyHealthThreshold(HextechEnemyHexContext context, Creature target, uint combatId)
	{
		if (context.Tracking.EscapePlanTriggered.Add(combatId))
		{
			context.Tracking.EscapePlanPending.Add(combatId);
		}

		return Task.CompletedTask;
	}
}
