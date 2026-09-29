namespace HextechRunes;

internal sealed class VitalitySurgeEnemyHex : HextechEnemyHexEffect
{
	internal const int HpPerPercentPerPlayer = 20;
	private const int MaxBonusPercent = 30;

	internal override MonsterHexKind Kind => MonsterHexKind.VitalitySurge;

	internal override int EnemyHealOrder => 25;

	internal override decimal ModifyDamageMultiplicative(HextechEnemyHexContext context, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return dealer == null ? 1m : ResolveMultiplier(dealer.MaxHp, context.ScalingPlayerCount);
	}

	internal override decimal ModifyBlockMultiplicative(HextechEnemyHexContext context, Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return ResolveMultiplier(target.MaxHp, context.ScalingPlayerCount);
	}

	internal override decimal ModifyEnemyHealMultiplicative(HextechEnemyHexContext context, Creature creature, decimal amount)
	{
		return ResolveMultiplier(creature.MaxHp, context.ScalingPlayerCount);
	}

	internal static decimal ResolveMultiplier(decimal maxHp, int playerCount = 1)
	{
		return EnemyMaxHpStepMultiplier.Resolve(maxHp, HpPerPercentPerPlayer, playerCount, MaxBonusPercent);
	}
}
