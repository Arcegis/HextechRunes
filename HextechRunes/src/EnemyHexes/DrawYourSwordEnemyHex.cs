namespace HextechRunes;

internal sealed class DrawYourSwordEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.DrawYourSword;

	internal override int PersistentOrder => 50;

	internal override decimal ModifyDamageMultiplicative(HextechEnemyHexContext context, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return 1.4m;
	}

	internal override async Task ApplyPersistentToEnemy(HextechEnemyHexContext context, Creature creature, int? maxHpBaseOverride, bool replayOneShotPowers)
	{
		if (HextechCombatProcTracker.TryMarkPersistentHexApplied(context.Tracking.DrawYourSwordApplied, creature, replayOneShotPowers))
		{
			await PowerCmd.Apply<ImbalancedPower>(creature, 1m, creature, null);
		}
	}
}
