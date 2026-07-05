namespace HextechRunes;

public sealed class SendThemInRune : HextechRelicBase
{
	private int _generatedMinionsThisCombat;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
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

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || !IsRegentPlayer(player))
		{
			return;
		}

		int ordinal = ConsumeCombatProcOrdinal(nameof(SendThemInRune), ref _generatedMinionsThisCombat);
		CardModel card = HextechStableRandom.CreateMinionCard(combatState, Owner, "send-them-in", ordinal);

		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
	}
}
