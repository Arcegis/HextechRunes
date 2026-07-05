namespace HextechRunes;

public sealed class BreadAndButterRune : FirstTypedCardReplayRuneBase
{
	protected override CardType TargetCardType => CardType.Attack;

	public override async Task AfterObtained()
	{
		await BreadSandwichAssemblyHelper.TryAssemble(Owner);
	}
}
