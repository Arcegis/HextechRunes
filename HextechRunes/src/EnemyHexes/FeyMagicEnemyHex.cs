namespace HextechRunes;

internal sealed class FeyMagicEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.FeyMagic;

	internal override async Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		foreach (KeyValuePair<uint, uint> pending in context.Tracking.FeyMagicPendingNoDrawPlayers.ToList())
		{
			uint combatId = pending.Key;
			Creature? creature = combatState.GetCreature(combatId);
			Creature? source = combatState.GetCreature(pending.Value);
			context.Tracking.FeyMagicPendingNoDrawPlayers.Remove(combatId);
			if (creature == null || !creature.IsAlive || creature.Side != CombatSide.Player)
			{
				continue;
			}

			await PowerCmd.Apply<NoDrawPower>(creature, 1m, source, null);
		}
	}

	internal override Task AfterEnemyDamageGivenPlayerHit(HextechEnemyHexContext context, Creature dealer, Creature target)
	{
		if (target.CombatId != null
			&& dealer.CombatId != null
			&& !context.Tracking.FeyMagicPendingNoDrawPlayers.ContainsKey(target.CombatId.Value))
		{
			context.Tracking.FeyMagicPendingNoDrawPlayers[target.CombatId.Value] = dealer.CombatId.Value;
		}

		return Task.CompletedTask;
	}
}
