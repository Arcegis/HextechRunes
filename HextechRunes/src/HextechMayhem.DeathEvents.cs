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
		if (!HasActiveMonsterHex(MonsterHexKind.GetExcited)
			|| creature.Side != CombatSide.Enemy
			|| creature.CombatState?.RunState != RunState)
		{
			return;
		}

		PainfulStabsPower? painfulStabs = creature.GetPower<PainfulStabsPower>();
		if (painfulStabs != null)
		{
			await PowerCmd.Remove(painfulStabs);
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

		if (HasActiveMonsterHex(MonsterHexKind.Nightstalking))
		{
			IReadOnlyList<Creature> enemies = GetAliveEnemies(combatState)
				.Where(enemy => enemy != target)
				.ToList();
			if (enemies.Count > 0)
			{
				await PowerCmd.Apply<StrengthPower>(enemies, 1m, null, null);
				await PowerCmd.Apply<PaperCutsPower>(enemies, 1m, null, null);
			}
		}

		if (HasActiveMonsterHex(MonsterHexKind.GetExcited))
		{
			IReadOnlyList<Creature> enemies = GetAliveEnemies(combatState)
				.Where(enemy => enemy != target)
				.ToList();
			if (enemies.Count > 0)
			{
				await PowerCmd.Apply<StrengthPower>(enemies, 1m, null, null);
				await PowerCmd.Apply<PainfulStabsPower>(enemies, 1m, null, null);
			}
		}
	}
}
