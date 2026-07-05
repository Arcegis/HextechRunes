namespace HextechRunes;

public sealed class AncientWineRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new HealVar(1m)
	];

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsOwnedSkill(cardPlay.Card))
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.Heal(Owner!.Creature, DynamicVars.Heal.BaseValue);
	}
}
