namespace HextechRunes;

public sealed class CantTouchThisRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("MinCost", 2m),
		new PowerVar<BufferPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<BufferPower>()
	];

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null || !IsOwnedCardWithEffectiveCostAtLeast(cardPlay.Card, DynamicVars["MinCost"].BaseValue))
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<BufferPower>(Owner.Creature, DynamicVars["BufferPower"].BaseValue, Owner.Creature, null);
	}
}
