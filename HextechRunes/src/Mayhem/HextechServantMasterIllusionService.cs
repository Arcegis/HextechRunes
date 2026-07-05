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
