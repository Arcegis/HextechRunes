namespace HextechRunes;

internal sealed class ManipulateRealityEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.ManipulateReality;

	internal static int ModifyEnemyStatusCardCount(HextechEnemyHexContext context, int count)
	{
		return count > 0 && context.IsActive(MonsterHexKind.ManipulateReality)
			? count * 2
			: count;
	}
}
