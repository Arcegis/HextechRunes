namespace HextechRunes;

public sealed class UltimateUnstoppableRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
		[
			new DynamicVar("MinCost", 2m),
			new PowerVar<ArtifactPower>(2m)
		];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<ArtifactPower>()
	];

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null || !IsOwnedCardWithEffectiveCostAtLeast(cardPlay.Card, DynamicVars["MinCost"].BaseValue))
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<ArtifactPower>(Owner.Creature, DynamicVars["ArtifactPower"].BaseValue, Owner.Creature, cardPlay.Card);
	}
}
