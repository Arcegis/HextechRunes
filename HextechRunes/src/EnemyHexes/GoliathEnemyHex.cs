namespace HextechRunes;

internal sealed class GoliathEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Goliath;

	internal override int PersistentOrder => 10;

	internal override int EnemyHealOrder => 10;

	internal override decimal ModifyDamageMultiplicative(HextechEnemyHexContext context, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return 1.2m;
	}

	internal override decimal ModifyBlockMultiplicative(HextechEnemyHexContext context, Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return 1.2m;
	}

	internal override decimal ModifyEnemyHealAmount(HextechEnemyHexContext context, Creature creature, decimal amount)
	{
		return amount * 1.2m;
	}

	internal override async Task ApplyPersistentToEnemy(HextechEnemyHexContext context, Creature creature, int? maxHpBaseOverride, bool replayOneShotPowers)
	{
		if (HextechMayhemModifier.TryMarkPersistentHexApplied(context.Tracking.GoliathApplied, creature, replayOneShotPowers))
		{
			await HextechMayhemModifier.EnsureMonsterMaxHpBonus(creature, 0.3m, maxHpBaseOverride);
			context.UpdateEnemyScale(creature);
		}
	}
}
