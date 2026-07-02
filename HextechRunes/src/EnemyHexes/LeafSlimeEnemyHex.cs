namespace HextechRunes;

internal sealed class LeafSlimeEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.LeafSlime;

	internal override async Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		// "每过 N 回合"约定（照 DivineIntervention）：N=3/2/1 → RoundNumber % (N+1)。
		int interval = context.TierValue(Kind, 3, 2, 1) + 1;
		if (combatState.RoundNumber <= 1 || combatState.RoundNumber % interval != 0)
		{
			return;
		}

		foreach (Player player in players
			.Where(static creature => !creature.IsDead)
			.Select(static creature => creature.Player)
			.OfType<Player>()
			.OrderBy(static player => player.NetId))
		{
			CardModel slimed = combatState.CreateCard<Slimed>(player);
			await HextechCardGeneration.AddGeneratedCardToCombat(
				slimed,
				PileType.Discard,
				addedByPlayer: false,
				CardPilePosition.Top);
		}
	}
}
