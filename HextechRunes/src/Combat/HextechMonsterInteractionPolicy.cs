using System.Diagnostics.CodeAnalysis;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace HextechRunes;

internal static class HextechMonsterInteractionPolicy
{
	public static bool IsTrueCombatDeath(Creature creature)
	{
		return IsTrueCombatDeath(creature, out _);
	}

	public static bool IsTrueCombatDeath(Creature creature, [NotNullWhen(true)] out HextechCombatState? combatState)
	{
		combatState = creature.CombatState;
		return combatState != null
			&& !IsBossPhaseTransitionDeath(creature)
			&& Hook.ShouldCreatureBeRemovedFromCombatAfterDeath(combatState, creature);
	}

	public static bool IsBossPhaseTransitionDeath(Creature creature)
	{
		HextechCombatState? combatState = creature.CombatState;
		return combatState != null
			&& creature.Monster is TestSubject
			&& !Hook.ShouldCreatureBeRemovedFromCombatAfterDeath(combatState, creature);
	}

	public static bool ShouldIgnoreMonsterSelfBuff(PowerModel power)
	{
		return IsStructuralMonsterBuff(power);
	}

	public static bool IsStructuralMonsterBuff(PowerModel power)
	{
		return power is SandpitPower
			or ReattachPower
			or AdaptablePower;
	}
}
