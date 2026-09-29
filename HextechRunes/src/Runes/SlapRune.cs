namespace HextechRunes;

public sealed class SlapRune : LimitedDebuffProcRelicBase
{
	protected override bool HasTurnLimit => false;

	protected override bool ListensToOwnerDebuffs => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(1m)
	];

	protected override Task OnDebuffProc(Player owner, Creature target)
	{
		return PowerCmd.Apply<StrengthPower>(owner.Creature, DynamicVars.Strength.BaseValue, owner.Creature, null);
	}
}
