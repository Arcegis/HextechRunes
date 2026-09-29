namespace HextechRunes;

internal sealed class ProteinShakeEnemyHex : HextechEnemyHexEffect
{
	internal const int HpPerPercentPerPlayer = 5;

	internal override MonsterHexKind Kind => MonsterHexKind.ProteinShake;

	internal override int EnemyHealOrder => 30;

	internal override decimal ModifyBlockMultiplicative(HextechEnemyHexContext context, Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return ResolveMultiplier(target.MaxHp, context.ScalingPlayerCount);
	}

	internal override decimal ModifyEnemyHealMultiplicative(HextechEnemyHexContext context, Creature creature, decimal amount)
	{
		return ResolveMultiplier(creature.MaxHp, context.ScalingPlayerCount);
	}

	// 没有上限。
	internal static decimal ResolveMultiplier(decimal maxHp, int playerCount = 1)
	{
		return EnemyMaxHpStepMultiplier.Resolve(maxHp, HpPerPercentPerPlayer, playerCount);
	}
}
