namespace HextechRunes;

public sealed class ShrinkRayRune : HextechRelicBase
{
	private bool _applyingShrinkRay;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<ShrinkPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ShrinkPower>()
	];

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (_applyingShrinkRay
			|| Owner == null
			|| target.Side != CombatSide.Enemy
			|| result.UnblockedDamage <= 0
			|| !IsDamageFromOwner(dealer, cardSource))
		{
			return;
		}

		Flash([target]);
		_applyingShrinkRay = true;
		try
		{
			await PowerCmd.Apply<ShrinkPower>(target, DynamicVars["ShrinkPower"].BaseValue, Owner.Creature, cardSource);
		}
		finally
		{
			_applyingShrinkRay = false;
		}
	}
}
