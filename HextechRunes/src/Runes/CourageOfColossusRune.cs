namespace HextechRunes;

public sealed class CourageOfColossusRune : LimitedDebuffProcRelicBase
{
	protected override int MaxProcsPerTurn => 2;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Plating", 3m)
	];

	protected override Task OnDebuffProc(Player owner, Creature target)
	{
		return PowerCmd.Apply<PlatingPower>(owner.Creature, DynamicVars["Plating"].BaseValue, owner.Creature, null);
	}
}
