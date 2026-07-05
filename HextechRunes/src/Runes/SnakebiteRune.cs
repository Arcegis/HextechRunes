namespace HextechRunes;

public sealed class SnakebiteRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Snakebite>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || combatState.RoundNumber > 1)
		{
			return;
		}

		CardModel card = combatState.CreateCard<Snakebite>(Owner);
		card.SetToFreeThisCombat();
		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
	}
}
