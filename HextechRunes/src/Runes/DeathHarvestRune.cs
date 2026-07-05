namespace HextechRunes;

public sealed class DeathHarvestRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("HealPercent", 50m)
	];

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| Owner.Creature.CombatState?.RoundNumber != 1
			|| target.Side != CombatSide.Enemy
			|| result.UnblockedDamage <= 0
			|| !IsDamageFromOwner(dealer, cardSource))
		{
			return;
		}

		int heal = FloorToInt(result.UnblockedDamage * DynamicVars["HealPercent"].BaseValue / 100m);
		if (heal <= 0)
		{
			return;
		}

		Flash([Owner.Creature]);
		await CreatureCmd.Heal(Owner.Creature, heal);
	}
}
