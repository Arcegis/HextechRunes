namespace HextechRunes;

internal sealed class StartupRoutineEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.StartupRoutine;

	internal override Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		return CreatureCmd.GainBlock(enemy, context.TierValue(Kind, 10, 15, 20), ValueProp.Unpowered, null);
	}
}
