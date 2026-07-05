namespace HextechRunes;

public sealed class TormentorRune : LimitedDebuffProcRelicBase
{
	private bool _applyingBurnProc;

	protected override int MaxProcsPerTurn => 1;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<HextechBurnPower>()
	];

#if STS2_104_OR_NEWER
	public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#else
	public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
#endif
	{
		if (_applyingBurnProc)
		{
			return;
		}

#if STS2_104_OR_NEWER
		await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
#else
		await base.AfterPowerAmountChanged(power, amount, applier, cardSource);
#endif
	}

	protected override async Task OnEnemyDebuffApplied(Creature target)
	{
		try
		{
			_applyingBurnProc = true;
			await PowerCmd.Apply<HextechBurnPower>(target, 4m, Owner!.Creature, null);
		}
		finally
		{
			_applyingBurnProc = false;
		}
	}
}
