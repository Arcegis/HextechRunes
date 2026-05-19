namespace HextechRunes;

internal sealed class FeelTheBurnEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.FeelTheBurn;

	internal override Task AfterEnemyHealthThreshold(HextechEnemyHexContext context, Creature target, uint combatId)
	{
		if (context.Tracking.FeelTheBurnTriggered.Add(combatId))
		{
			context.Tracking.FeelTheBurnPending.Add(combatId);
		}

		return Task.CompletedTask;
	}

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		if (context.Tracking.FeelTheBurnPending.Count == 0 || players.Count == 0)
		{
			return;
		}

		foreach (uint combatId in context.Tracking.FeelTheBurnPending.ToList())
		{
			Creature? creature = combatState.GetCreature(combatId);
			context.Tracking.FeelTheBurnPending.Remove(combatId);
			if (creature == null || !creature.IsAlive)
			{
				continue;
			}

			await context.RunGroupedPlayerDebuffBurst(async () =>
			{
				await PowerCmd.Apply<WeakPower>(players, 1m, creature, null);
				await PowerCmd.Apply<VulnerablePower>(players, 1m, creature, null);
				await PowerCmd.Apply<HextechBurnPower>(players, context.TierValue(Kind, 3, 4, 5), creature, null);
			});
		}
	}
}
