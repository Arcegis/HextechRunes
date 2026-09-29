namespace HextechRunes;

internal sealed class MyteEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.Myte;

	internal override async Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		int count = context.TierValue(Kind, 0, 1, 2);
		if (count <= 0)
		{
			return;
		}

		foreach (Player player in HextechEnemyHexContext.GetAlivePlayersByNetId(players))
		{
			for (int i = 0; i < count; i++)
			{
				CardModel toxic = combatState.CreateCard<Toxic>(player);
				await HextechCardGeneration.AddGeneratedCardToCombat(
					toxic,
					PileType.Hand,
					addedByPlayer: false,
					CardPilePosition.Top);
			}
		}
	}
}
