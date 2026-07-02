namespace HextechRunes;

internal sealed class VantomEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Vantom;

	private const decimal MaxHpPerStack = 25m;

	internal override Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		int stacks = (int)Math.Floor(enemy.MaxHp / MaxHpPerStack);
		return stacks > 0
			? PowerCmd.Apply<SlipperyPower>(enemy, stacks, enemy, null)
			: Task.CompletedTask;
	}
}
