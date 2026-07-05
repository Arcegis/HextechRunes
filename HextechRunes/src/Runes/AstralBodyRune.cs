namespace HextechRunes;

public sealed class AstralBodyRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("MaxHpPercent", 50m),
		new DynamicVar("DamageMultiplier", 0.9m)
	];

	public override Task AfterObtained()
	{
		decimal gain = Math.Max(1m, Math.Floor(Owner!.Creature.MaxHp * DynamicVars["MaxHpPercent"].BaseValue / 100m));
		return CreatureCmd.GainMaxHp(Owner.Creature, gain);
	}

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (!IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource))
		{
			return 1m;
		}

		return DynamicVars["DamageMultiplier"].BaseValue;
	}
}
