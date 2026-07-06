namespace HextechRunes;

internal sealed class VantomEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Vantom;

	private const decimal MaxHpPerStack = 25m;

	internal override Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		// 按联机缩放后的最终血量结算,并走裸 Apply(不进玩家数缩放路径),层数不因人数再翻倍。
		int stacks = (int)Math.Floor(enemy.MaxHp / MaxHpPerStack);
		return stacks > 0
			? PowerCmd.Apply<SlipperyPower>(enemy, stacks, enemy, null)
			: Task.CompletedTask;
	}
}
