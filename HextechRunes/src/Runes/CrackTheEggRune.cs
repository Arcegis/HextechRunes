namespace HextechRunes;

public sealed class CrackTheEggRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(9m, ValueProp.Unpowered)
	];

	public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		if (Owner == null || Owner.Creature.IsDead || side != Owner.Creature.Side || Owner.Creature.Block <= 0)
		{
			return;
		}

		List<Creature> enemies = combatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}

		Flash(enemies);
		foreach (Creature enemy in enemies)
		{
			await HextechGameApiCompat.Damage(choiceContext, enemy, DynamicVars.Damage.BaseValue, ValueProp.Unpowered, Owner.Creature, null);
		}
	}
}
