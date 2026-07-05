namespace HextechRunes;

public sealed class SomethingFromNothingRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null || cardPlay.Card.Owner != Owner || !cardPlay.Card.Keywords.Contains(CardKeyword.Ethereal))
		{
			return Task.CompletedTask;
		}

		Flash();
		return CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner, fromHandDraw: false);
	}
}
