namespace HextechRunes;

public sealed class PlasterRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new SummonVar(1m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task AfterSummon(PlayerChoiceContext choiceContext, Player summoner, decimal amount)
	{
		if (summoner != Owner
			|| Owner == null
			|| Owner.Creature.IsDead
			|| amount <= 0m
			|| !Owner.IsOstyAlive
			|| Owner.Osty == null)
		{
			return Task.CompletedTask;
		}

		Flash([Owner.Osty]);
		return CreatureCmd.Heal(Owner.Osty, amount);
	}
}
