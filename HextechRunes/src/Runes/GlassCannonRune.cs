namespace HextechRunes;

public sealed class GlassCannonRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DamageMultiplier", 1.5m),
		new DynamicVar("HealCapPercent", 0.7m)
	];

	public decimal HealCapPercent => DynamicVars["HealCapPercent"].BaseValue;

	public override async Task AfterObtained()
	{
		if (Owner?.Creature == null)
		{
			return;
		}

		int hpCap = Math.Max(1, FloorToInt(Owner.Creature.MaxHp * HealCapPercent));
		if (Owner.Creature.CurrentHp > hpCap)
		{
			await CreatureCmd.SetCurrentHp(Owner.Creature, hpCap);
		}
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
