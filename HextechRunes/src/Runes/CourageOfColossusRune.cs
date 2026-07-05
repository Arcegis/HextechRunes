namespace HextechRunes;

public sealed class CourageOfColossusRune : LimitedDebuffProcRelicBase
{
	protected override int MaxProcsPerTurn => 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Plating", 3m)
	];

	protected override Task OnEnemyDebuffApplied(Creature target)
	{
		return PowerCmd.Apply<PlatingPower>(Owner!.Creature, DynamicVars["Plating"].BaseValue, Owner!.Creature, null);
	}
}
