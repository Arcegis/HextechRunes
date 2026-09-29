namespace HextechRunes;

internal sealed class TungstenRodEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.TungstenRod;

	internal override decimal ModifyHpLostAfterOsty(
		HextechEnemyHexContext context,
		Creature target,
		decimal amount,
		ValueProp props,
		Creature? dealer,
		CardModel? cardSource)
	{
		// 分发层已限定 target 在本局战斗中。
		if (target.Side != CombatSide.Enemy
			|| target.IsDead
			|| amount <= 0m)
		{
			return amount;
		}

		return ReduceHpLoss(amount, context.TierValue(Kind, 1, 2, 3));
	}

	internal static decimal ReduceHpLoss(decimal amount, int reduction)
	{
		return amount <= 0m
			? amount
			: Math.Max(0m, amount - Math.Max(0, reduction));
	}
}
