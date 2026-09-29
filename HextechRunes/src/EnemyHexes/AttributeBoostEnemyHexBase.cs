namespace HextechRunes;

/// <summary>属性加成类敌方海克斯（数值、数值叠数值、数值叠数值叠数值）：伤害、格挡、回复与最大生命同一比例，数值见 <see cref="EnemyAttributeBoostValues"/>。</summary>
internal abstract class AttributeBoostEnemyHexBase : HextechEnemyHexEffect, IHextechEnemyMaxHpCoefficientProvider
{
	internal sealed override int PersistentOrder => 35;

	internal sealed override decimal ModifyDamageMultiplicative(HextechEnemyHexContext context, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) => EnemyAttributeBoostValues.GetMultiplier(Kind, context);

	internal sealed override decimal ModifyBlockMultiplicative(HextechEnemyHexContext context, Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay) => EnemyAttributeBoostValues.GetMultiplier(Kind, context);

	internal sealed override decimal ModifyEnemyHealMultiplicative(HextechEnemyHexContext context, Creature creature, decimal amount) => EnemyAttributeBoostValues.GetMultiplier(Kind, context);

	internal sealed override Task ApplyPersistentToEnemy(HextechEnemyHexContext context, Creature creature, int? maxHpBaseOverride, bool replayOneShotPowers) => EnemyAttributeBoostValues.ApplyPersistent(Kind, context, creature, maxHpBaseOverride, replayOneShotPowers);

	public decimal GetMaxHpBonusFraction(HextechEnemyHexContext context, Creature creature) => EnemyAttributeBoostValues.GetBonusFraction(Kind, context);
}
