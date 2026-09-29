namespace HextechRunes;

internal sealed class HeavyHitterEnemyHex : HextechEnemyHexEffect
{
	internal const int HpPerPercentPerPlayer = 15;
	private const int MaxBonusPercent = 30;

	internal override MonsterHexKind Kind => MonsterHexKind.HeavyHitter;

	internal override decimal ModifyDamageMultiplicative(HextechEnemyHexContext context, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return dealer == null ? 1m : ResolveMultiplier(dealer.MaxHp, context.ScalingPlayerCount);
	}

	internal static decimal ResolveMultiplier(decimal maxHp, int playerCount = 1)
	{
		return EnemyMaxHpStepMultiplier.Resolve(maxHp, HpPerPercentPerPlayer, playerCount, MaxBonusPercent);
	}
}
