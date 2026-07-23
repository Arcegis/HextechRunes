namespace HextechRunes;

internal sealed class VitalitySurgeEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.VitalitySurge;

	internal override int EnemyHealOrder => 25;

	internal override decimal ModifyDamageMultiplicative(HextechEnemyHexContext context, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return dealer == null ? 1m : ResolveMultiplier(dealer.MaxHp);
	}

	internal override decimal ModifyBlockMultiplicative(HextechEnemyHexContext context, Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return ResolveMultiplier(target.MaxHp);
	}

	internal override decimal ModifyEnemyHealAmount(HextechEnemyHexContext context, Creature creature, decimal amount)
	{
		return amount * ResolveMultiplier(creature.MaxHp);
	}

	internal static decimal ResolveMultiplier(decimal maxHp)
	{
		decimal twentyHpSteps = Math.Max(0m, Math.Floor(maxHp / 20m));
		return 1m + twentyHpSteps * 0.01m;
	}
}
