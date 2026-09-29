namespace HextechRunes;

// 自有 Modifier 基类:0.108.0 伤害管线与 0.109.0 卡牌打出去向的签名桥,以及回合钩子的 participants 透传
// (Modifier 没有持有者,默认不过滤,需要时覆盖 *ForParticipants 入口)。
internal abstract class HextechModifierBase : ModifierModel
{
	public virtual decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return 1m;
	}

#if STS2_108_OR_NEWER
	public sealed override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		return ModifyDamageMultiplicativeCompat(target, amount, props, dealer, cardSource);
	}
#else
	public sealed override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return ModifyDamageMultiplicativeCompat(target, amount, props, dealer, cardSource);
	}
#endif

	// 0.109.0 起卡牌打出去向从 (PileType, CardPilePosition) 元组改为 CardLocation:同 HextechRelicBase 的桥。
	public virtual (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPositionCompat(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
	{
		return (pileType, position);
	}

#if STS2_109_OR_NEWER
	public sealed override CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocation cardLocation)
	{
		(PileType pileType, CardPilePosition position) = ModifyCardPlayResultPileTypeAndPositionCompat(card, isAutoPlay, resources, cardLocation.pileType, cardLocation.position);
		cardLocation.pileType = pileType;
		cardLocation.position = position;
		return cardLocation;
	}
#else
	public sealed override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
	{
		return ModifyCardPlayResultPileTypeAndPositionCompat(card, isAutoPlay, resources, pileType, position);
	}
#endif

	public virtual Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, HextechCombatState combatState)
	{
		return Task.CompletedTask;
	}

	// Modifier 没有持有者，不能像 Relic/Power 那样统一跳过；需要区分"谁在这次回合里"的实现覆盖
	// 带 participants 的入口自己筛（队友额外回合只带那名玩家重入回合钩子）。
	public virtual Task BeforeSideTurnStartForParticipants(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, HextechCombatState combatState)
	{
		return BeforeSideTurnStart(choiceContext, side, combatState);
	}

	public sealed override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, HextechCombatState combatState)
	{
		return BeforeSideTurnStartForParticipants(choiceContext, side, participants, combatState);
	}

	public virtual Task AfterSideTurnStart(CombatSide side, HextechCombatState combatState)
	{
		return Task.CompletedTask;
	}

	// 同 BeforeSideTurnStartForParticipants：默认忽略 participants，需要时覆盖此入口自己筛。
	public virtual Task AfterSideTurnStartForParticipants(CombatSide side, IReadOnlyList<Creature> participants, HextechCombatState combatState)
	{
		return AfterSideTurnStart(side, combatState);
	}

	public sealed override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, HextechCombatState combatState)
	{
		return AfterSideTurnStartForParticipants(side, participants, combatState);
	}

	public virtual Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		return Task.CompletedTask;
	}

	public virtual Task BeforeTurnEndForParticipants(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		return BeforeTurnEnd(choiceContext, side);
	}

	public sealed override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		return BeforeTurnEndForParticipants(choiceContext, side, participants);
	}

	public virtual Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		return Task.CompletedTask;
	}

	public virtual Task AfterTurnEndForParticipants(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		return AfterTurnEnd(choiceContext, side);
	}

	public sealed override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		return AfterTurnEndForParticipants(choiceContext, side, participants);
	}
}
