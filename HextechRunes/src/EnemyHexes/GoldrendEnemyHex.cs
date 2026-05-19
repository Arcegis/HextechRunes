namespace HextechRunes;

internal sealed class GoldrendEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Goldrend;

	internal override decimal ModifyDamageMultiplicative(HextechEnemyHexContext context, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return 1m + context.TierValue(Kind, 0.05m, 0.10m, 0.15m);
	}

	internal override Task AfterEnemyDamageGivenImmediate(HextechEnemyHexContext context, Creature dealer, DamageResult result, Creature target, CardModel? cardSource)
	{
		return result.UnblockedDamage > 0 && target.Player != null
			? HextechGoldrendSync.HandleEnemyGoldrendHit(target.Player)
			: Task.CompletedTask;
	}
}
