namespace HextechRunes;

internal sealed class StartupRoutineEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.StartupRoutine;

	internal override Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		return CreatureCmd.GainBlock(enemy, 15m, ValueProp.Unpowered, null);
	}
}
