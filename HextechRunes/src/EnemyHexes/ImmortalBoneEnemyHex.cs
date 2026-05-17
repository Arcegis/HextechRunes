namespace HextechRunes;

internal sealed class ImmortalBoneEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.ImmortalBone;

	internal override int PersistentOrder => 110;

	internal override async Task ApplyPersistentToEnemy(HextechEnemyHexContext context, Creature creature, int? maxHpBaseOverride, bool replayOneShotPowers)
	{
		if (creature.GetPowerAmount<HardenedShellPower>() <= 0m)
		{
			int shell = Math.Max(12, (int)Math.Floor(creature.MaxHp * 0.6m));
			await HextechEnemyPowerScalingHooks.Apply<HardenedShellPower>(creature, shell, creature, null);
		}
	}
}
