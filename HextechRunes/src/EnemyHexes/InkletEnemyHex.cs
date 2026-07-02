namespace HextechRunes;

internal sealed class InkletEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Inklet;

	internal override Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		return PowerCmd.Apply<SlipperyPower>(enemy, context.TierValue(Kind, 1, 2, 3), enemy, null);
	}
}
