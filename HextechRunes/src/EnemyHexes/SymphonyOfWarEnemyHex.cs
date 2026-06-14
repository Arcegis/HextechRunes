namespace HextechRunes;

internal sealed class SymphonyOfWarEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.SymphonyOfWar;

	internal override Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		int ritual = context.TierValue(Kind, 0, 1, 2);
		return ritual > 0
			? PowerCmd.Apply<RitualPower>(enemy, ritual, enemy, null)
			: Task.CompletedTask;
	}
}
