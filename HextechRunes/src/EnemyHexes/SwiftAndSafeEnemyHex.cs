namespace HextechRunes;

internal sealed class SwiftAndSafeEnemyHex : DrawProgressEnemyHexBase
{
	internal override MonsterHexKind Kind => MonsterHexKind.SwiftAndSafe;

	private int GetCardsPerArtifact(HextechEnemyHexContext context)
	{
		return context.TierValue(Kind, 15, 12, 10);
	}

	protected override async Task AfterLocalCardDrawn(HextechEnemyHexContext context, Player owner, HextechCombatState combatState)
	{
		if (HextechEnemyDrawProgress.RecordDraw(context.Tracking.SwiftAndSafePlayerCardsDrawnThisCombat, owner, GetCardsPerArtifact(context)) == 0)
		{
			return;
		}

		foreach (Creature enemy in context.GetAliveEnemies(combatState))
		{
			await PowerCmd.Apply<ArtifactPower>(enemy, 1, enemy, null);
		}
	}

	protected override async Task ResolveDrawProgressFromHistory(HextechEnemyHexContext context, HextechCombatState combatState)
	{
		int cardsPerArtifact = GetCardsPerArtifact(context);

		// 人工制品节奏按「单人」标定,不随联机人数放大:阈值随玩家数等比放大,改用全队合计抽牌数对
		// (阈值 × 玩家数)结算。等价于按全队「人均抽牌数」给层数,而非各玩家分别跨阈值后求和——后者
		// 会让层数随人数线性翻倍(5 人 ≈ 5 倍),正是玩家反馈的 bug。各玩家累计抽牌数仍精确存进 tracking,
		// 故跨越阈值的小数进度不会丢失。
		int playerCount = Math.Max(1, combatState.Players.Count);
		int threshold = cardsPerArtifact * playerCount;

		int totalDrawnNow = 0;
		int totalDrawnPrev = 0;
		foreach (Player player in combatState.Players.OrderBy(static player => player.NetId))
		{
			int drawnCards = HextechCombatHistoryHelper.CountOwnedCardsDrawn(player);
			int previousDrawnCards = context.Tracking.SwiftAndSafePlayerCardsDrawnThisCombat.GetValueOrDefault(player.NetId, 0);

			// 防御:历史计数不应回退;万一回退就按已记录值,避免负增量。
			if (drawnCards < previousDrawnCards)
			{
				drawnCards = previousDrawnCards;
			}

			totalDrawnNow += drawnCards;
			totalDrawnPrev += previousDrawnCards;
			context.Tracking.SwiftAndSafePlayerCardsDrawnThisCombat[player.NetId] = drawnCards;
		}

		int pendingArtifact = HextechRelicBase.CountThresholdCrossings(totalDrawnPrev, totalDrawnNow, threshold);
		if (pendingArtifact <= 0)
		{
			return;
		}

		foreach (Creature enemy in context.GetAliveEnemies(combatState))
		{
			await PowerCmd.Apply<ArtifactPower>(enemy, pendingArtifact, enemy, null);
		}
	}
}
