namespace HextechRunes;

public sealed class DizzySpinningRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Dazed>()
	];

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return player == Owner ? count + DynamicVars.Cards.BaseValue : count;
	}

	public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
	{
		if (shuffler != Owner || Owner == null || Owner.Creature.IsDead || Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		CardModel dazed = combatState.CreateCard<Dazed>(Owner);
		Flash();
		await HextechCardGeneration.AddGeneratedCardToCombat(dazed, PileType.Draw, addedByPlayer: true, CardPilePosition.Random);
	}
}
