namespace HextechRunes;

public sealed class AdvanceToRetreatRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(3m, ValueProp.Unpowered)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<VulnerablePower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| target.Side != CombatSide.Enemy
			|| result.TotalDamage <= 0
			|| !IsOwnedAttack(cardSource)
			|| target.GetPowerAmount<VulnerablePower>() <= 0m)
		{
			return;
		}

		Flash([target]);
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
	}
}
