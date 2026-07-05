namespace HextechRunes;

internal static class HextechAutoPlayHelper
{
	// "受管临时牌":由效果临时创建/克隆、经 AutoPlayTransientCardAndCleanup 打出后强制离场的牌
	// (自动巡逻的扫荡凝视、中和强化的分身等)。万用瞄准镜这类"打出后把牌送回手牌"的效果必须
	// 跳过它们:牌回手后会被下面的 finally 移除,模型没了但手牌 UI 残留一张点不动的幽灵牌。
	// 联机确定性:注册/注销都发生在两端一致执行的战斗逻辑内,任一时刻各端集合状态一致。
	private static readonly HashSet<CardModel> _transientCards = [];

	internal static bool IsTransientAutoPlayCard(CardModel card)
	{
		return _transientCards.Contains(card);
	}

	internal static async Task AutoPlayTransientCardAndCleanup(
		PlayerChoiceContext choiceContext,
		CardModel card,
		Creature? target,
		AutoPlayType type = AutoPlayType.Default,
		bool skipXCapture = false,
		bool skipCardPileVisuals = false)
	{
		_transientCards.Add(card);
		try
		{
			await AutoPlayOrMoveToResultPile(choiceContext, card, target, type, skipXCapture, skipCardPileVisuals);
		}
		finally
		{
			_transientCards.Remove(card);
			if (card.Pile?.Type == PileType.Hand)
			{
				// 兜底:牌打完仍回到手牌(例如第三方效果送回)。这里不能 skipVisuals,
				// 原版 RemoveFromCombat(skipVisuals:true) 会跳过 NCard 清理,留下幽灵牌。
				await CardPileCmd.RemoveFromCombat(card);
			}
		}
	}

	internal static async Task AutoPlayOrMoveToResultPile(
		PlayerChoiceContext choiceContext,
		CardModel card,
		Creature? target,
		AutoPlayType type = AutoPlayType.Default,
		bool skipXCapture = false,
		bool skipCardPileVisuals = false)
	{
		try
		{
			await CardCmd.AutoPlay(choiceContext, card, target, type, skipXCapture, skipCardPileVisuals);
		}
		catch (Exception ex) when (IsKnownExternalAutoPlayCompatibilityFailure(ex))
		{
			Log.Warn($"[{ModInfo.Id}][AutoPlay] Skipped autoplay for {card.Id} after external compatibility failure: {ex.GetType().Name}: {ex.Message}");
			await MoveToResultPile(choiceContext, card);
		}
	}

	private static bool IsKnownExternalAutoPlayCompatibilityFailure(Exception ex)
	{
		return ex is TypeLoadException or MissingMethodException
			|| ex is AggregateException aggregate && aggregate.InnerExceptions.Any(IsKnownExternalAutoPlayCompatibilityFailure);
	}

	private static async Task MoveToResultPile(PlayerChoiceContext choiceContext, CardModel card)
	{
		await card.MoveToResultPileWithoutPlaying(choiceContext);
	}
}
