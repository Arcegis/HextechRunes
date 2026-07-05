namespace HextechRunes;

public sealed class NeowsGrudgeRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<NeowsFury>(),
		HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
	];

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		CardModel card = combatState.CreateCard<NeowsFury>(Owner);
		card.AddKeyword(CardKeyword.Ethereal);
		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
	}
}
