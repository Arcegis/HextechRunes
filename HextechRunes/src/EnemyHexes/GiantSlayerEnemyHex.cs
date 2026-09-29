namespace HextechRunes;

internal sealed class GiantSlayerEnemyHex : HextechEnemyHexEffect
{
	// 敌方版让敌人体型缩小（纯视觉，呼应「体型变小」的设定，无机制意义）。
	internal const float BodyScaleShrink = 0.25f;

	internal const int PlayerMaxHpPerPercent = 2;
	internal const decimal MaxBonus = 1.00m;

	internal override MonsterHexKind Kind => MonsterHexKind.GiantSlayer;

	internal override decimal ModifyDamageMultiplicative(HextechEnemyHexContext context, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target?.Player == null || dealer == null)
		{
			return 1m;
		}

		return 1m + GetBonus((int)target.MaxHp);
	}

	internal static decimal GetBonus(int playerMaxHp)
	{
		return playerMaxHp <= 0
			? 0m
			: Math.Min(MaxBonus, playerMaxHp / PlayerMaxHpPerPercent * 0.01m);
	}

	internal override Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		// 体型缩小(纯视觉,无机制意义),呼应「巨人杀手」体型变小的设定。
		context.UpdateEnemyScale(enemy);
		return Task.CompletedTask;
	}
}
