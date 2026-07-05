namespace HextechRunes;

public sealed class MindOverMatterRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
	];

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, HextechCombatState combatState)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		List<CardModel> pool = Owner.Character.CardPool
			.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
			.Where(static card => card.Rarity is not CardRarity.Basic and not CardRarity.Ancient && card.CanBeGeneratedInCombat)
			.ToList();
		if (pool.Count == 0)
		{
			return;
		}

		CardModel canonicalCard = HextechStableRandom.Pick(
			pool,
			(RunState)Owner.RunState,
			HextechStableRandom.CardKey,
			"mind-over-matter",
			HextechStableRandom.PlayerKey(Owner),
			combatState.RoundNumber.ToString(),
			CountOwnedCardsDrawnFromHistory().ToString());
		CardModel card = combatState.CreateCard(canonicalCard, Owner);
		card.AddKeyword(CardKeyword.Ethereal);
		card.SetToFreeThisCombat();

		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
	}
}
