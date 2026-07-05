namespace HextechRunes;

public sealed class AdamantRune : LimitedDebuffProcRelicBase
{
	protected override int MaxProcsPerTurn => 5;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(3m, ValueProp.Unpowered)
	];

	protected override Task OnEnemyDebuffApplied(Creature target)
	{
		return CreatureCmd.GainBlock(Owner!.Creature, DynamicVars.Block, null);
	}
}
