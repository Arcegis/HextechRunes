namespace HextechRunes;

public sealed class PlateletRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(3m, ValueProp.Unpowered)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (Owner == null
			|| creature != Owner.Creature
			|| delta >= 0m
			|| Owner.Creature.IsDead
			|| !HextechSts2Compat.IsPartOfPlayerTurn(Owner))
		{
			return Task.CompletedTask;
		}

		decimal block = Math.Floor(-delta) * DynamicVars.Block.BaseValue;
		if (block <= 0m)
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Unpowered, null);
	}
}
