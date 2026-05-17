using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

internal sealed partial class HextechMayhemModifier
{
	public override async Task BeforeDeath(Creature creature)
	{
		HextechEnemyHexContext context = new(this);
		foreach (HextechEnemyHexEffect effect in HextechEnemyHexEffects.GetActive(this))
		{
			await effect.BeforeDeath(context, creature);
		}
	}

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (wasRemovalPrevented
			|| target.Side != CombatSide.Enemy
			|| !HextechMonsterInteractionPolicy.IsTrueCombatDeath(target, out HextechCombatState? combatState))
		{
			return;
		}

		HextechEnemyHexContext context = new(this);
		foreach (HextechEnemyHexEffect effect in HextechEnemyHexEffects.GetActive(this))
		{
			await effect.AfterDeath(context, choiceContext, target, combatState);
		}
	}
}
