namespace HextechRunes;

internal sealed class GiantSlayerEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.GiantSlayer;

	internal override decimal ModifyDamageMultiplicative(HextechEnemyHexContext context, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target?.Player == null || dealer == null)
		{
			return 1m;
		}

		// 玩家最大生命每比该敌人高 6 点,敌人对你的伤害 +1%,最多 +100%。
		int diff = (int)(target.MaxHp - dealer.MaxHp);
		if (diff <= 0)
		{
			return 1m;
		}

		decimal bonus = Math.Min(1.00m, diff / 6 * 0.01m);
		return 1m + bonus;
	}

	internal override Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		// 体型缩小(纯视觉,无机制意义),呼应「巨人杀手」体型变小的设定。
		context.UpdateEnemyScale(enemy);
		return Task.CompletedTask;
	}
}
