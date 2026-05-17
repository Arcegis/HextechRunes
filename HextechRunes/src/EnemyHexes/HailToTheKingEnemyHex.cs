namespace HextechRunes;

internal sealed class HailToTheKingEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.HailToTheKing;

	internal override async Task ApplyCombatStartToEnemy(HextechEnemyHexContext context, Creature enemy, CombatRoom room)
	{
		if (room.RoomType is not (RoomType.Elite or RoomType.Boss))
		{
			return;
		}

		int sustain = Math.Max(1, (int)Math.Floor(enemy.MaxHp * 0.05m));
		await HextechEnemyPowerScalingHooks.Apply<ArtifactPower>(enemy, 3m, enemy, null);
		await HextechEnemyPowerScalingHooks.Apply<PlatingPower>(enemy, sustain, enemy, null);
		await HextechEnemyPowerScalingHooks.Apply<RegenPower>(enemy, sustain, enemy, null);
	}
}
