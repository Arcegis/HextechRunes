using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

namespace HextechRunes;

internal static class HextechServantMasterIllusionService
{
	public static async Task TryApply(
		RunState runState,
		HextechMayhemCombatTrackingState tracking,
		Creature creature,
		Creature? applier,
		CardModel? cardSource)
	{
		if (tracking.HandlingServantMasterIllusion
			|| creature.Side != CombatSide.Enemy
			|| !creature.IsAlive
			|| creature.CombatState?.RunState != runState
			|| !creature.HasPower<MinionPower>()
			|| creature.HasPower<IllusionPower>())
		{
			return;
		}

		try
		{
			tracking.HandlingServantMasterIllusion = true;
			await PowerCmd.Apply<IllusionPower>(creature, 1m, applier ?? creature, cardSource);
		}
		finally
		{
			tracking.HandlingServantMasterIllusion = false;
		}
	}
}
