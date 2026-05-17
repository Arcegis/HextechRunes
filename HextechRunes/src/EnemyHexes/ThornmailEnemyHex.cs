namespace HextechRunes;

internal sealed class ThornmailEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Thornmail;

	internal override int PersistentOrder => 70;

	internal override async Task ApplyPersistentToEnemy(HextechEnemyHexContext context, Creature creature, int? maxHpBaseOverride, bool replayOneShotPowers)
	{
		if (HextechMayhemModifier.TryMarkPersistentHexApplied(context.Tracking.ThornmailApplied, creature, replayOneShotPowers))
		{
			await HextechEnemyPowerScalingHooks.Apply<ReflectPower>(creature, 5m, creature, null);
		}
	}
}
