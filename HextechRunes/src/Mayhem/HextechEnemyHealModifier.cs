using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace HextechRunes;

internal static class HextechEnemyHealModifier
{
	public static decimal Modify(HextechMayhemModifier modifier, Creature creature, decimal amount)
	{
		if (creature.Side != CombatSide.Enemy)
		{
			return amount;
		}

		HextechEnemyHexContext context = new(modifier);
		foreach (HextechEnemyHexEffect effect in HextechEnemyHexEffects.GetActive(modifier).OrderBy(static effect => effect.EnemyHealOrder))
		{
			amount = effect.ModifyEnemyHealAmount(context, creature, amount);
		}

		return amount;
	}
}
