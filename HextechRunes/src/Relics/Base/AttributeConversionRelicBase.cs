namespace HextechRunes;

public abstract class AttributeConversionRelicBase : HextechRelicBase
{
	private bool _isConverting;
	private decimal? _pendingAmount;
	private Creature? _pendingApplier;

	/// <summary>
	/// 是否转换这种属性。同时用于施加前的规范 Power（TryModifyPowerAmountReceived）与已施加的实例（AfterPowerAmountChanged）。
	/// </summary>
	protected abstract bool ShouldConvert(PowerModel power);

	protected abstract Task ApplyConvertedPower(Creature owner, decimal amount, Creature? applier, CardModel? cardSource);

	protected abstract Task RevertOriginalPower(Creature owner, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource);

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_pendingAmount = null;
		_pendingApplier = null;
		_isConverting = false;
		return Task.CompletedTask;
	}

	public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
	{
		modifiedAmount = amount;
		if (_isConverting || Owner == null || target != Owner.Creature || amount == 0m || !ShouldConvert(canonicalPower))
		{
			return false;
		}

		// 施加管线结束后再把原属性换成转换后的属性；这条路径拿不到来源卡牌。
		_pendingAmount = amount;
		_pendingApplier = applier;
		modifiedAmount = 0m;
		return true;
	}

	public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
	{
		if (_pendingAmount is not decimal amount)
		{
			return;
		}

		Creature? applier = _pendingApplier;
		_pendingAmount = null;
		_pendingApplier = null;
		if (Owner == null)
		{
			return;
		}

		_isConverting = true;
		try
		{
			Flash();
			await ApplyConvertedPower(Owner.Creature, amount, applier, null);
		}
		finally
		{
			_isConverting = false;
		}
	}

	public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (_isConverting || Owner == null || amount == 0m || power.Owner != Owner.Creature || !ShouldConvert(power))
		{
			return;
		}

		_isConverting = true;
		try
		{
			Flash();
			await RevertOriginalPower(Owner.Creature, power, amount, applier, cardSource);
			await ApplyConvertedPower(Owner.Creature, amount, applier, cardSource);
		}
		finally
		{
			_isConverting = false;
		}
	}
}
