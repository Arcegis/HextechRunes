namespace HextechRunes;

internal sealed class JudicatorEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Judicator;

	internal override decimal ModifyDamageMultiplicative(HextechEnemyHexContext context, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		// 仅在攻击玩家、且玩家当前生命低于最大生命 50% 时加伤。
		if (target?.Player == null || target.CurrentHp * 2m >= target.MaxHp)
		{
			return 1m;
		}

		return 1m + context.TierValue(Kind, 0.10m, 0.20m, 0.30m);
	}
}
