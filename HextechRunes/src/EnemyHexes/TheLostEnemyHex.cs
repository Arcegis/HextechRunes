namespace HextechRunes;

internal sealed class TheLostEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.TheLost;

	internal override async Task ApplyCombatStartPlayerDebuffs(HextechEnemyHexContext context, CombatRoom room, IReadOnlyList<Creature> players)
	{
		int loss = context.TierValue(Kind, 0, 1, 2);
		if (loss <= 0)
		{
			return;
		}

		await PowerCmd.Apply<StrengthPower>(players, -loss, null, null);
	}
}
