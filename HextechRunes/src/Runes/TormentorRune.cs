namespace HextechRunes;

public sealed class TormentorRune : LimitedDebuffProcRelicBase
{
	// 文案写的是字面值，改数值要同步九语言。
	private const decimal BurnPerDebuff = 2m;

	private bool _applyingBurnProc;

	protected override bool HasTurnLimit => false;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechBurnPower>()
	];

	public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (_applyingBurnProc)
		{
			return;
		}

		await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
	}

	protected override async Task OnDebuffProc(Player owner, Creature target)
	{
		try
		{
			_applyingBurnProc = true;
			await PowerCmd.Apply<HextechBurnPower>(target, BurnPerDebuff, owner.Creature, null);
		}
		finally
		{
			_applyingBurnProc = false;
		}
	}
}
