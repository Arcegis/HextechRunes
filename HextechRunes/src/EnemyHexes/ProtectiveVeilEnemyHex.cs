namespace HextechRunes;

internal sealed class ProtectiveVeilEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.ProtectiveVeil;

	internal override int PersistentOrder => 60;

	internal override async Task ApplyPersistentToEnemy(HextechEnemyHexContext context, Creature creature, int? maxHpBaseOverride, bool replayOneShotPowers)
	{
		if (HextechMayhemModifier.TryMarkPersistentHexApplied(context.Tracking.ProtectiveVeilApplied, creature, replayOneShotPowers))
		{
			await HextechEnemyPowerScalingHooks.Apply<ArtifactPower>(creature, HextechMayhemModifier.ProtectiveVeilInitialArtifactStacks, creature, null);
		}
	}

	internal override async Task BeforeEnemySideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players, IReadOnlyList<Creature> enemies)
	{
		if (context.Tracking.EnemyProtectiveVeilTurnCounter % 2 != 0)
		{
			return;
		}

		foreach (Creature enemy in enemies)
		{
			await HextechEnemyPowerScalingHooks.Apply<ArtifactPower>(enemy, 1m, enemy, null);
		}
	}
}
