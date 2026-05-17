namespace HextechRunes;

internal sealed class AstralBodyEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.AstralBody;

	internal override int PersistentOrder => 20;

	internal override decimal ModifyDamageMultiplicative(HextechEnemyHexContext context, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return 0.9m;
	}

	internal override async Task ApplyPersistentToEnemy(HextechEnemyHexContext context, Creature creature, int? maxHpBaseOverride, bool replayOneShotPowers)
	{
		if (HextechMayhemModifier.TryMarkPersistentHexApplied(context.Tracking.AstralBodyApplied, creature, replayOneShotPowers))
		{
			await HextechMayhemModifier.EnsureMonsterMaxHpBonus(creature, 0.3m, maxHpBaseOverride);
		}
	}
}
