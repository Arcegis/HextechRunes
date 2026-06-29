namespace HextechRunes;

internal sealed class NearDeathFeastEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.NearDeathFeast;

	internal override async Task AfterEnemyHealthThreshold(HextechEnemyHexContext context, Creature target, uint combatId)
	{
		if (!target.IsAlive || !context.Tracking.NearDeathFeastTriggered.Add(combatId))
		{
			return;
		}

		int heal = Math.Max(1, (int)Math.Floor(target.MaxHp * 0.20m));
		await CreatureCmd.Heal(target, heal);
		await PowerCmd.Apply<StrengthPower>(target, context.TierValue(Kind, 1, 2, 3), target, null);
	}
}
