namespace HextechRunes;

internal sealed class InkletEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Inklet;

	internal override Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		// 走缩放路径:联机按玩家数放大(N/2N/3N),描述侧由 PlayerCountScaledStacks 同步显示。
		return HextechEnemyPowerScalingHooks.Apply<SlipperyPower>(enemy, context.TierValue(Kind, 1, 2, 3), enemy, null);
	}
}
