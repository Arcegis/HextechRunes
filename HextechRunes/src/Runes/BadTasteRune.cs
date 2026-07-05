namespace HextechRunes;

public sealed class BadTasteRune : LimitedDebuffProcRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new HealVar(1m)
	];

	protected override Task OnEnemyDebuffApplied(Creature target)
	{
		return CreatureCmd.Heal(Owner!.Creature, DynamicVars.Heal.BaseValue);
	}
}
