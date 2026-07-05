namespace HextechRunes;

public sealed class PiggyBankRune : HextechRelicBase, IHextechSharedCombatVictoryRune
{
	private int _counter;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCounter
	{
		get => _counter;
		set
		{
			_counter = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool HasUponPickupEffect => true;

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount => _counter;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new GoldVar(200),
		new DynamicVar("CounterGain", 20m)
	];

	public override Task AfterObtained()
	{
		return Owner == null ? Task.CompletedTask : PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
	}

	public override Task BeforeCombatStart()
	{
		SavedCounter = 0;
		return Task.CompletedTask;
	}

	public override Task AfterDamageReceived(
		PlayerChoiceContext choiceContext,
		Creature target,
		DamageResult result,
		ValueProp props,
		Creature? dealer,
		CardModel? cardSource)
	{
		if (Owner == null
			|| target != Owner.Creature
			|| result.UnblockedDamage <= 0m)
		{
			return Task.CompletedTask;
		}

		SavedCounter += DynamicVars["CounterGain"].IntValue;
		Flash();
		return Task.CompletedTask;
	}

	public override Task AfterCombatVictory(CombatRoom room)
	{
		if (IsNetworkMultiplayer())
		{
			return Task.CompletedTask;
		}

		return ApplySharedCombatVictory(room);
	}

	public Task ApplySharedCombatVictory(CombatRoom room)
	{
		if (Owner == null || _counter <= 0)
		{
			SavedCounter = 0;
			return Task.CompletedTask;
		}

		HextechGoldRewardHelper.AddFixedExtraGoldReward(room, Owner, _counter);
		Flash(Array.Empty<Creature>());
		SavedCounter = 0;
		return Task.CompletedTask;
	}
}
