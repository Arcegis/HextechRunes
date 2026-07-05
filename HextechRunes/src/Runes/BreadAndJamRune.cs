namespace HextechRunes;

public sealed class BreadAndJamRune : FirstTypedCardReplayRuneBase
{
	protected override CardType TargetCardType => CardType.Skill;

	public override async Task AfterObtained()
	{
		await BreadSandwichAssemblyHelper.TryAssemble(Owner);
	}
}
