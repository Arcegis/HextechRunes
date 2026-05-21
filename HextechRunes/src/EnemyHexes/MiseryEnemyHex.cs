namespace HextechRunes;

internal sealed class MiseryEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Misery;

	internal override async Task ApplyCombatStartPlayerDebuffs(HextechEnemyHexContext context, CombatRoom room, IReadOnlyList<Creature> players)
	{
		int strengthLoss = context.TierValue(Kind, 1, 1, 2);
		int dexterityLoss = context.TierValue(Kind, 0, 1, 1);
		if (strengthLoss <= 0 && dexterityLoss <= 0)
		{
			return;
		}

		await context.RunGroupedPlayerDebuffBurst(async () =>
		{
			if (strengthLoss > 0)
			{
				await PowerCmd.Apply<StrengthPower>(players, -strengthLoss, null, null);
			}

			if (dexterityLoss > 0)
			{
				await PowerCmd.Apply<DexterityPower>(players, -dexterityLoss, null, null);
			}
		});
	}
}
