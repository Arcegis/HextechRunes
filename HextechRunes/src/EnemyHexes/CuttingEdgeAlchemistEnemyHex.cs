namespace HextechRunes;

internal sealed class CuttingEdgeAlchemistEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.CuttingEdgeAlchemist;

	internal static bool IsActiveFor(Player player)
	{
		return player.RunState is RunState runState
			&& HextechMayhemModifier.FindIn(runState) is HextechMayhemModifier modifier
			&& modifier.HasActiveMonsterHex(MonsterHexKind.CuttingEdgeAlchemist);
	}
}
