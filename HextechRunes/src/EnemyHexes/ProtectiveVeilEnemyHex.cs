namespace HextechRunes;

internal sealed class ProtectiveVeilEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.ProtectiveVeil;

	internal override int PersistentOrder => 60;

	internal override async Task ApplyPersistentToEnemy(HextechEnemyHexContext context, Creature creature, int? maxHpBaseOverride, bool replayOneShotPowers)
	{
		if (HextechCombatProcTracker.TryMarkPersistentHexApplied(context.Tracking.ProtectiveVeilApplied, creature, replayOneShotPowers))
		{
			await HextechEnemyPowerScalingHooks.Apply<ArtifactPower>(creature, context.TierValue(Kind, 1, 2, 3), creature, null);
		}
	}
}
