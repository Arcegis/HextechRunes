namespace HextechRunes;

internal sealed class MikaelsBlessingEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.MikaelsBlessing;

	internal override async Task AfterEnemyHealthThreshold(HextechEnemyHexContext context, Creature target, uint combatId)
	{
		if (context.Tracking.MikaelsBlessingTriggers.GetValueOrDefault(combatId, 0) >= HextechMayhemModifier.MikaelsBlessingMaxTriggers)
		{
			return;
		}

		context.Tracking.MikaelsBlessingTriggers[combatId] = context.Tracking.MikaelsBlessingTriggers.GetValueOrDefault(combatId, 0) + 1;
		decimal healPercent = context.TierValue(Kind, 0.10m, 0.25m, 0.40m);
		int heal = Math.Max(1, (int)Math.Floor(target.MaxHp * healPercent));
		HextechMikaelsBlessingVfx.Play(target);
		await CreatureCmd.Heal(target, heal);

		List<PowerModel> negativePowers = target.Powers
			.Where(static power => power.GetTypeForAmount(power.Amount) == PowerType.Debuff)
			.ToList();
		foreach (PowerModel power in negativePowers)
		{
			await PowerCmd.Remove(power);
		}
	}
}
