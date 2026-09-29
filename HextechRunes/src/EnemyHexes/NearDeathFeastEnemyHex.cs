namespace HextechRunes;

/// <summary>
/// 敌方「濒死狂宴」:与玩家版同构的不死机制——敌人生命低于 1 时进入濒死(负血、禁疗禁格挡、
/// 每 1 负血 1 力量),负血达到最大生命 5%/10%/15%(按层级)时才真正死亡。
/// 状态机由 <see cref="HextechEnemyNearDeath"/> 经 LoseHp/CurrentHp/IsAlive 等通用拦截层实现;
/// 本类只在生命变化后等待力量补差(拦截层是同步前缀,不能在那里发命令)。
/// </summary>
internal sealed class NearDeathFeastEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.NearDeathFeast;

	internal override Task AfterCurrentHpChanged(HextechEnemyHexContext context, Creature creature, decimal delta)
	{
		return HextechEnemyNearDeath.SyncStrengthAfterHpChanged(creature);
	}
}
