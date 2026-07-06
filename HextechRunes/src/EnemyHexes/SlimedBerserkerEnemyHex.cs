namespace HextechRunes;

internal sealed class SlimedBerserkerEnemyHex : HextechEnemyHexEffect
{
	internal override MonsterHexKind Kind => MonsterHexKind.SlimedBerserker;

	internal override async Task BeforePlayerSideTurnStart(HextechEnemyHexContext context, HextechCombatState combatState, IReadOnlyList<Creature> players)
	{
		// "战斗开始时":首个玩家回合开始只发生一次,牌堆此时已就绪。
		// 额外回合不推进 RoundNumber 且回合开始 hook 会重入,按回合防重。
		if (combatState.RoundNumber != 1
			|| HextechCombatProcTracker.ConsumeGlobalProcInCombat(context.Tracking, $"round-once:{Kind}:{combatState.RoundNumber}") > 0)
		{
			return;
		}

		int count = context.TierValue(Kind, 5, 10, 15);
		foreach (Player player in players
			.Where(static creature => !creature.IsDead)
			.Select(static creature => creature.Player)
			.OfType<Player>()
			.OrderBy(static player => player.NetId))
		{
			for (int i = 0; i < count; i++)
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
}
