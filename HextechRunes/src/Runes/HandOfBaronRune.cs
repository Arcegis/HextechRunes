namespace HextechRunes;

public sealed class HandOfBaronRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("DamageMultiplier", 1.2m),
		new DynamicVar("Shrink", 2m)
	];

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource) ? DynamicVars["DamageMultiplier"].BaseValue : 1m;
	}

	public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner == null || side != Owner.Creature.Side)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<ShrinkPower>(combatState.HittableEnemies, DynamicVars["Shrink"].BaseValue, Owner.Creature, null);
	}
}
