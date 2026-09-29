namespace HextechRunes;

internal sealed class BrutalForceEnemyHex : HextechEnemyHexEffect
{
	private const decimal BlockPercent = 0.08m;

	internal override MonsterHexKind Kind => MonsterHexKind.BrutalForce;

	internal override async Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		await PowerCmd.Apply<StrengthPower>(enemy, 1m, enemy, null);
		int block = Math.Max(1, (int)Math.Floor(enemy.MaxHp * BlockPercent));
		await CreatureCmd.GainBlock(enemy, block, ValueProp.Unpowered, null);
	}
}
