namespace HextechRunes;

public sealed class CircleOfDeathRune : HextechRelicBase
{
	private int _sustainDamageTargetsThisCombat;

	public Task HandleSustainGained(decimal amount)
	{
		if (Owner == null ||
			Owner.Creature.IsDead ||
			Owner.Creature.CombatState == null ||
			!CombatManager.Instance.IsInProgress ||
			amount <= 0m)
		{
			return Task.CompletedTask;
		}

		int damage = FloorToInt(amount);
		if (damage <= 0)
		{
			return Task.CompletedTask;
		}

		List<Creature> enemies = Owner.Creature.CombatState.HittableEnemies
			.Where(static enemy => enemy.IsAlive)
			.ToList();
		if (enemies.Count == 0)
		{
			return Task.CompletedTask;
		}

		int targetOrdinal = ConsumeCombatProcOrdinal(nameof(CircleOfDeathRune), ref _sustainDamageTargetsThisCombat);
		Creature target = enemies[HextechStableRandom.Index(
			(RunState)Owner.RunState,
			enemies.Count,
			"circle-of-death-target",
			HextechStableRandom.PlayerKey(Owner),
			Owner.Creature.CombatState.RoundNumber.ToString(),
			damage.ToString(),
			targetOrdinal.ToString())];
		Flash([target]);
		HextechCombatVfx.DeathRingLash(Owner.Creature, target);
		return HextechGameApiCompat.Damage(new BlockingPlayerChoiceContext(), target, damage, ValueProp.Unpowered, Owner.Creature, null);
	}

	public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
		return creature == Owner?.Creature ? HandleSustainGained(amount) : Task.CompletedTask;
	}
}
