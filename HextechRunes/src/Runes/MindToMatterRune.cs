namespace HextechRunes;

public sealed class MindToMatterRune : HextechRelicBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new MaxHpVar(1m)
	];

	public override Task AfterObtained()
	{
		if (Owner == null)
		{
			return Task.CompletedTask;
		}

		int maxHpGain = Owner.Deck.Cards.Count;
		if (maxHpGain <= 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.GainMaxHp(Owner.Creature, maxHpGain);
	}
}
