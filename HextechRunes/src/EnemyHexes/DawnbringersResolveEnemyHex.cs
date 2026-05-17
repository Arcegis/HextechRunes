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

		int regen = Math.Max(1, (int)Math.Floor(target.MaxHp * HextechMayhemModifier.DawnbringersResolveRegenPercent));
		await HextechEnemyPowerScalingHooks.Apply<RegenPower>(target, regen, target, null);
	}
}
