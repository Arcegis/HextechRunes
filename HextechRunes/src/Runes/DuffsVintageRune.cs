namespace HextechRunes;

public sealed class DuffsVintageRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CostReduction", 1m)
	];

	public override bool ShouldFlush(Player player)
	{
		return player != Owner;
	}

	public override Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (Owner == null || side != Owner.Creature.Side || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		List<CardModel> cards = PileType.Hand.GetPile(Owner).Cards
			.Where(CanReduceCost)
			.ToList();
		if (cards.Count == 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		foreach (CardModel card in cards)
		{
			int reduction = DynamicVars["CostReduction"].IntValue;
			if (!card.EnergyCost.CostsX)
			{
				int currentCostBeforeGlobalModifiers = card.EnergyCost.GetWithModifiers(CostModifiers.Local);
				int nextCost = Math.Max(0, currentCostBeforeGlobalModifiers - reduction);
				card.EnergyCost.SetUntilPlayed(nextCost, reduceOnly: true);
			}

			// 星辉牌:用与能量同一套「直到打出」的临时费用机制减费(原版 SetStarCostUntilPlayed →
			// TemporaryCardCost.UntilPlayed,打出时随其它 until-played 临时费一并清除),逐回合留在手累减、
			// 与描述「回合结束时仍在手的牌费用-1 直到打出」及能量路径一致。
			// 此前星辉减费走持续型 TryModifyStarCost 修饰(按 (RoundNumber-1) 每次查询都减),会持续整场战斗、
			// 打出后也不消耗——已移除。
			if (!card.HasStarCostX && card.CurrentStarCost > 0)
			{
				int nextStarCost = Math.Max(0, card.CurrentStarCost - reduction);
				if (nextStarCost != card.CurrentStarCost)
				{
					card.SetStarCostUntilPlayed(nextStarCost);
				}
			}
		}

		return Task.CompletedTask;
	}

	private static bool CanReduceCost(CardModel card)
	{
		return !card.EnergyCost.CostsX || (!card.HasStarCostX && card.CurrentStarCost > 0);
	}
}
