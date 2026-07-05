namespace HextechRunes;

public sealed class BreadAndCheeseRune : FirstTypedCardReplayRuneBase
{
	protected override CardType TargetCardType => CardType.Power;

	public override async Task AfterObtained()
	{
		await BreadSandwichAssemblyHelper.TryAssemble(Owner);
	}
}
