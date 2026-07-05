namespace HextechRunes;

public sealed class TanksShieldRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(3m, ValueProp.Unpowered)
	];

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsOwnedAttack(cardPlay.Card) || Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
	}
}
