namespace HextechRunes;

// 双刀流的效果(攻击伤害白值减半向上取整、段数加倍)需要在攻击执行前改写 AttackCommand 的
// _damagePerHit 与 _hitCount,标准 effect hook 无法表达,因此实现放在
// HextechCombatHooks.DualWieldAttackCommandExecutePrefix。本类仅用于注册与图鉴/描述登记。
internal sealed class DualWieldEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.DualWield;
}
