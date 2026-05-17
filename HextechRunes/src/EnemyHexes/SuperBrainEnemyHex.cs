namespace HextechRunes;

internal sealed class SuperBrainEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.SuperBrain;

	internal override int PersistentOrder => 80;

	internal override async Task ApplyPersistentToEnemy(HextechEnemyHexContext context, Creature creature, int? maxHpBaseOverride, bool replayOneShotPowers)
	{
		if (!HextechMayhemModifier.TryMarkPersistentHexApplied(context.Tracking.SuperBrainApplied, creature, replayOneShotPowers))
		{
			return;
		}

		int plating = (int)Math.Floor(creature.MaxHp * 0.04m);
		if (plating > 0)
		{
			await HextechEnemyPowerScalingHooks.Apply<PlatingPower>(creature, plating, creature, null);
		}
	}
}
