namespace HextechRunes;

public sealed class RenewalRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
	{
		if (!IsOwnedCard(card) || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner, fromHandDraw: false);
	}
}
