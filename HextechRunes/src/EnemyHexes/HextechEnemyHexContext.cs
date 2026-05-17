namespace HextechRunes;

internal readonly struct HextechEnemyHexContext(HextechMayhemModifier modifier)
{
	internal HextechMayhemModifier Modifier => modifier;

	internal RunState RunState => modifier.ActiveRunState;

	internal HextechMayhemCombatTrackingState Tracking => modifier.CombatTracking;

	internal bool IsActive(MonsterHexKind kind)
	{
		return modifier.HasActiveMonsterHex(kind);
	}

	internal IReadOnlyList<Creature> GetAliveEnemies(HextechCombatState combatState)
	{
		return HextechMayhemModifier.GetAliveEnemies(combatState);
	}

	internal IReadOnlyList<Creature> GetAlivePlayerSideCreatures(HextechCombatState combatState)
	{
		return HextechMayhemModifier.GetAlivePlayerSideCreatures(combatState);
	}

	internal Task RunGroupedPlayerDebuffBurst(Func<Task> action)
	{
		return modifier.RunGroupedPlayerDebuffBurst(action);
	}

	internal Task TryApplyServantMasterIllusion(Creature creature, Creature? applier, CardModel? cardSource)
	{
		return modifier.TryApplyServantMasterIllusion(creature, applier, cardSource);
	}

	internal void UpdateEnemyScale(Creature creature)
	{
		modifier.UpdateEnemyScale(creature);
	}
}
