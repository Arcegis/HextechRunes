namespace HextechRunes;

internal sealed class ShrinkerBeetleEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.ShrinkerBeetle;

	internal override async Task ApplyCombatStartPlayerDebuffs(HextechEnemyHexContext context, CombatRoom room, IReadOnlyList<Creature> players)
	{
		await PowerCmd.Apply<ShrinkPower>(players, context.TierValue(Kind, 1, 2, 3), null, null);
	}
}
