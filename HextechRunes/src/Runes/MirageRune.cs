namespace HextechRunes;

public sealed class MirageRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(1m, ValueProp.Unpowered)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<PoisonPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override Task AfterSideTurnStart(CombatSide side, HextechCombatState combatState)
	{
		if (Owner == null || side != Owner.Creature.Side || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		decimal block = combatState.HittableEnemies
			.Sum(static enemy => Math.Max(0m, enemy.GetPowerAmount<PoisonPower>()));
		if (block <= 0m)
		{
			return Task.CompletedTask;
		}

		Flash();
		return CreatureCmd.GainBlock(Owner.Creature, block * DynamicVars.Block.BaseValue, ValueProp.Unpowered, null);
	}
}
