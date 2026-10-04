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
		if (Owner.PlayerCombatState == null
			|| Owner.Creature.CombatState is not HextechCombatState combatState
			|| !CombatManager.Instance.IsInProgress
			|| CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}

		Flash();
		await AddRandomUpgradedDragonSoulCardsToCombatHand(Owner, DynamicVars.Cards.IntValue, combatState);
	}

	private static async Task AddRandomUpgradedDragonSoulCardsToCombatHand(Player owner, int count, HextechCombatState combatState)
	{
		if (count <= 0)
		{
			return;
		}

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
