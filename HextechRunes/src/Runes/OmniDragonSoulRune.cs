namespace HextechRunes;

public sealed class OmniDragonSoulRune : HextechRelicBase
{
	// 六种龙魂卡。下标即稳定随机的 roll 编号，顺序不能调整：同一种子下抽到哪张卡依赖它。
	private static readonly Func<CardModel>[] DragonSoulCards =
	[
		ModelDb.Card<OceanDragonSoulCard>,
		ModelDb.Card<InfernalDragonSoulCard>,
		ModelDb.Card<HextechDragonSoulCard>,
		ModelDb.Card<MountainDragonSoulCard>,
		ModelDb.Card<ChemtechDragonSoulCard>,
		ModelDb.Card<CloudDragonSoulCard>
	];

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(3)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. DragonSoulCards.Select(static card => HoverTipFactory.FromCard(card()))
	];

	public override async Task BeforeCombatStart()
	{
		// 排在前面的遗物(如烟花锻造器)可能已在开局打死全部主要敌人,战斗进入结束流程后不再发牌。
		if (Owner.PlayerCombatState == null
			|| CombatManager.Instance.IsOverOrEnding
			|| Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		Flash();
		await AddRandomUpgradedDragonSoulCardsToCombatHand(Owner, DynamicVars.Cards.IntValue, combatState);
	}

	private static async Task AddRandomUpgradedDragonSoulCardsToCombatHand(Player owner, int count, HextechCombatState combatState)
	{
		IReadOnlyList<int> dragonSoulRolls = RollDistinctDragonSoulCardKinds(owner, count, combatState);
		List<CardModel> cards = new(dragonSoulRolls.Count);
		foreach (int roll in dragonSoulRolls)
		{
			CardModel card = combatState.CreateCard(DragonSoulCards[roll](), owner);
			CardCmd.Upgrade(card);
			cards.Add(card);
		}

		await HextechCardGeneration.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true);
	}

	private static IReadOnlyList<int> RollDistinctDragonSoulCardKinds(Player owner, int count, HextechCombatState combatState)
	{
		return HextechStableRandom.PickDistinct(
			Enumerable.Range(0, DragonSoulCards.Length),
			count,
			(RunState)owner.RunState,
			static roll => roll.ToString(),
			"omni-dragon-soul-card",
			HextechStableRandom.PlayerKey(owner),
			combatState.RoundNumber.ToString(),
			owner.Deck.Cards.Count.ToString());
	}
}
