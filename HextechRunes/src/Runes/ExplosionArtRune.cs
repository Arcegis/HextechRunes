namespace HextechRunes;

public sealed class ExplosionArtRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("TurnStartCards", 1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<BigBang>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		int cardsToCreate = DynamicVars["TurnStartCards"].IntValue;
		if (cardsToCreate <= 0)
		{
			return;
		}

		Flash();
		List<CardModel> cards = new(cardsToCreate);
		for (int i = 0; i < cardsToCreate; i++)
		{
			cards.Add(combatState.CreateCard<BigBang>(Owner));
		}

		await HextechCardGeneration.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true);
	}
}
