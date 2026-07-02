namespace HextechRunes;

internal sealed class PhantasmalGardenerEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.PhantasmalGardener;

	internal override Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		int percent = context.TierValue(Kind, 8, 10, 12);
		int skittish = Math.Max(1, (int)Math.Floor(enemy.MaxHp * percent / 100m));
		return HextechEnemyPowerScalingHooks.Apply<SkittishPower>(enemy, skittish, enemy, null);
	}
}
