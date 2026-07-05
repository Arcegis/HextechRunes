namespace HextechRunes;

public sealed class FlawlessRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(3m, ValueProp.Unpowered)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsOwnedCard(cardPlay.Card) || Owner == null || Owner.Creature.IsDead || !ShouldCountCard(cardPlay.Card))
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
	}

	private static bool ShouldCountCard(CardModel card)
	{
		return HextechColorlessCardHelper.IsColorlessCard(card);
	}
}
