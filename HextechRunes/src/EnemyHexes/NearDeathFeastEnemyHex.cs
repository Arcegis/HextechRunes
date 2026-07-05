namespace HextechRunes;

/// <summary>
/// 敌方「濒死狂宴」:与玩家版同构的不死机制——敌人生命低于 1 时进入濒死(负血、禁疗禁格挡、
/// 每 1 负血 1 力量),负血达到最大生命 5%/10%/15%(按层级)时才真正死亡。
/// 机制全部由 <see cref="HextechEnemyNearDeath"/> 经 LoseHp/CurrentHp/IsAlive 等通用拦截层实现,
/// 本类仅作为海克斯激活集里的身份占位,无事件处理。
/// </summary>
internal sealed class NearDeathFeastEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.NearDeathFeast;
}
