namespace HextechRunes;

public sealed class BoneGuardRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("BlockMultiplier", 0.5m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task AfterSummon(PlayerChoiceContext choiceContext, Player summoner, decimal amount)
	{
		if (summoner != Owner || Owner == null || Owner.Creature.IsDead || amount <= 0m)
		{
			return Task.CompletedTask;
		}

		decimal block = Math.Floor(amount * DynamicVars["BlockMultiplier"].BaseValue);
		if (block <= 0m)
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Unpowered, null);
	}
}
