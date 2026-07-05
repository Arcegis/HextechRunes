namespace HextechRunes;

public sealed class FirebrandRune : HextechRelicBase
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechBurnPower>()
	];

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (Owner == null || target.Side != CombatSide.Enemy || !IsAttackDamageForRuneEffects(props, cardSource) || !IsDamageFromOwner(dealer, cardSource))
		{
			return;
		}

		await PowerCmd.Apply<HextechBurnPower>(target, 2m, Owner.Creature, cardSource);
	}
}
