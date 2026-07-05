namespace HextechRunes;

public sealed class DevilsDanceRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new HealVar(1m)
	];

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsOwnedAttack(cardPlay.Card) || Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
	}
}
