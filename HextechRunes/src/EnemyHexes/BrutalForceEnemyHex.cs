namespace HextechRunes;

internal sealed class BrutalForceEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.BrutalForce;

	internal override async Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		await PowerCmd.Apply<StrengthPower>(enemy, 1m, enemy, null);
		int block = Math.Max(1, (int)Math.Floor(enemy.MaxHp * HextechMayhemModifier.BrutalForceBlockPercent));
		await CreatureCmd.GainBlock(enemy, block, ValueProp.Unpowered, null);
	}
}
