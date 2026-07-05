namespace HextechRunes;

public sealed class UnsealedThroneRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override Task AfterStarsGained(int amount, Player gainer)
	{
		return HandleStarsChanged(amount, gainer);
	}

	public override Task AfterStarsSpent(int amount, Player spender)
	{
		return HandleStarsChanged(amount, spender);
	}

	private Task HandleStarsChanged(int amount, Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead || amount <= 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
	}
}
