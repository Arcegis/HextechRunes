namespace HextechRunes;

public sealed class CircleOfDeathRune : HextechRelicBase
{
	// 单机稳定随机的本地序号（见 ConsumeCombatProcOrdinal）：不在战斗开始清零，跨战斗累加、读档归零；
	// 联机改用 Mayhem 的每场计数。改成每场清零会改变单机的随机结果，按现状保留。
	private int _localTargetRollOrdinal;

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

		int targetOrdinal = ConsumeCombatProcOrdinal(nameof(CircleOfDeathRune), ref _localTargetRollOrdinal);
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
