namespace HextechRunes;

/// <summary>
/// 按玩家抽牌数累积的敌方海克斯（沃格莫特之灵、夜之锋刃、迅捷而安全）的共同样板：
/// 单机在每次抽牌时记一张；联机按战斗历史在两端都会经过的三个结算点（出牌后、玩家回合开始后、玩家回合结束前）补记，
/// 由子类保证重复结算不重复发奖。
/// </summary>
internal abstract class DrawProgressEnemyHexBase : HextechEnemyHexEffect
{
	internal sealed override Task AfterCardDrawn(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		// 分发层已限定本局战斗中的玩家侧卡牌。
		if (card.Owner is not Player owner
			|| owner.Creature.CombatState is not HextechCombatState combatState
			|| HextechPlayerContextHelper.IsNetworkMultiplayerRun())
		{
			return Task.CompletedTask;
		}

		return AfterLocalCardDrawn(context, owner, combatState);
	}

	internal sealed override Task AfterCardPlayedLate(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return HextechPlayerContextHelper.IsNetworkMultiplayerRun() && cardPlay.Card.Owner?.Creature.CombatState is HextechCombatState combatState
			? ResolveIfSameRun(context, combatState)
			: Task.CompletedTask;
	}

	internal sealed override Task AfterPlayerTurnStartLate(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, Player player)
	{
		return HextechPlayerContextHelper.IsNetworkMultiplayerRun() && player.Creature.CombatState is HextechCombatState combatState
			? ResolveIfSameRun(context, combatState)
			: Task.CompletedTask;
	}

	internal sealed override Task BeforeTurnEnd(HextechEnemyHexContext context, PlayerChoiceContext choiceContext, CombatSide side, CombatRoom? combatRoom)
	{
		return side == CombatSide.Player && combatRoom != null && HextechPlayerContextHelper.IsNetworkMultiplayerRun()
			? ResolveIfSameRun(context, combatRoom.CombatState)
			: Task.CompletedTask;
	}

	/// <summary>单机：玩家侧卡牌持有者抽到一张牌。</summary>
	protected abstract Task AfterLocalCardDrawn(HextechEnemyHexContext context, Player owner, HextechCombatState combatState);

	/// <summary>联机：按本场战斗历史补记全部玩家的抽牌数。</summary>
	protected abstract Task ResolveDrawProgressFromHistory(HextechEnemyHexContext context, HextechCombatState combatState);

	private Task ResolveIfSameRun(HextechEnemyHexContext context, HextechCombatState combatState)
	{
		return combatState.RunState == context.RunState
			? ResolveDrawProgressFromHistory(context, combatState)
			: Task.CompletedTask;
	}
}
