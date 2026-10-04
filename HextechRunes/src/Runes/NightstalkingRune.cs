namespace HextechRunes;

public sealed class NightstalkingRune : DrawThresholdRuneBase
{
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCardsDrawnThisCombat
	{
		get => SavedDrawProgress;
		set => SavedDrawProgress = value;
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CardsNeeded", 15m),
		new PowerVar<IntangiblePower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<IntangiblePower>()
	];

	protected override async Task ApplyDrawThresholdReward()
	{
		if (Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PowerCmd.Apply<IntangiblePower>(Owner.Creature, DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, null);
	}
}
