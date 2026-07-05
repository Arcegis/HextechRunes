namespace HextechRunes;

public sealed class RoyalTrialRune : HextechRelicBase
{
	private int _generatedMinionsThisCombat;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<SovereignBlade>(),
		HoverTipFactory.FromCard<MinionStrike>(),
		HoverTipFactory.FromCard<MinionDiveBomb>(),
		HoverTipFactory.FromCard<MinionSacrifice>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		_generatedMinionsThisCombat = 0;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_generatedMinionsThisCombat = 0;
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| !cardPlay.IsFirstInSeries
			|| cardPlay.IsAutoPlay
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card is not SovereignBlade
			|| Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		List<CardModel> cards = new(DynamicVars.Cards.IntValue);
		for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
		{
			cards.Add(CreateRandomMinionCard(combatState));
		}

		Flash();
		await HextechCardGeneration.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true);
	}

	private CardModel CreateRandomMinionCard(HextechCombatState combatState)
	{
		int ordinal = ConsumeCombatProcOrdinal(nameof(RoyalTrialRune), ref _generatedMinionsThisCombat);
		return HextechStableRandom.CreateMinionCard(
			combatState,
			Owner!,
			"royal-trial",
			ordinal);
	}
}
