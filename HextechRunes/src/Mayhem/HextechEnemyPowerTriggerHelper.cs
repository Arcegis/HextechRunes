using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

internal static class HextechEnemyPowerTriggerHelper
{
	public static bool TryGetMonsterDebuffTrigger(
		PowerModel power,
		decimal amount,
		Creature? applier,
		out Creature? target,
		out Creature? source)
	{
		target = power.Owner;
		source = applier;
		return amount > 0m
			&& target?.Side == CombatSide.Player
			&& source?.Side == CombatSide.Enemy
			&& power.GetTypeForAmount(amount) == PowerType.Debuff
			&& power is not ITemporaryPower;
	}

	public static bool TryGetMonsterSelfBuffTrigger(PowerModel power, decimal amount, Creature? applier, out Creature? source)
	{
		source = null;
		Creature? owner = power.Owner;
		if (amount <= 0m
			|| owner?.Side != CombatSide.Enemy
			|| power.GetTypeForAmount(amount) != PowerType.Buff
			|| HextechMonsterInteractionPolicy.ShouldIgnoreMonsterSelfBuff(power)
			|| power is ITemporaryPower
			|| power is PlatingPower
			|| power is BufferPower
			|| (applier != null && applier != owner))
		{
			return false;
		}

		source = owner;
		return true;
	}
}
