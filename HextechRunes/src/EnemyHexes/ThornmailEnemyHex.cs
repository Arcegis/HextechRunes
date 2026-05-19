namespace HextechRunes;

internal sealed class ThornmailEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Thornmail;

	internal override int PersistentOrder => 70;

	internal override async Task ApplyPersistentToEnemy(HextechEnemyHexContext context, Creature creature, int? maxHpBaseOverride, bool replayOneShotPowers)
	{
		if (HextechMayhemModifier.TryMarkPersistentHexApplied(context.Tracking.ThornmailApplied, creature, replayOneShotPowers))
		{
			await HextechEnemyPowerScalingHooks.Apply<ReflectPower>(creature, context.TierValue(Kind, 4, 5, 6), creature, null);
		}
	}
}
