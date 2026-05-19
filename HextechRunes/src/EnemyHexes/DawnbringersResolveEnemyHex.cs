namespace HextechRunes;

internal sealed class DawnbringersResolveEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.DawnbringersResolve;

	internal override async Task AfterEnemyHealthThreshold(HextechEnemyHexContext context, Creature target, uint combatId)
	{
		if (!context.Tracking.DawnTriggered.Add(combatId))
		{
			return;
		}

		decimal regenPercent = context.TierValue(Kind, 0.08m, 0.10m, 0.12m);
		int regen = Math.Max(1, (int)Math.Floor(target.MaxHp * regenPercent));
		await HextechEnemyPowerScalingHooks.Apply<RegenPower>(target, regen, target, null);
	}
}
