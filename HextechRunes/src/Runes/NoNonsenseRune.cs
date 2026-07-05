namespace HextechRunes;

public sealed class NoNonsenseRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<StrengthPower>(1m)
	];

	public async Task HandlePreventedNonHandDraw(int drawsPrevented)
	{
		if (Owner == null || Owner.Creature.IsDead || drawsPrevented <= 0)
		{
			return;
		}

		Flash();
		await Cmd.CustomScaledWait(0.05f, 0.1f);
	}
}
