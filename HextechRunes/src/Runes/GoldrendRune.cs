namespace HextechRunes;

public sealed class GoldrendRune : HextechRelicBase
{
	private int _countThisCombat;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("CountPerHit", 10m)
	];

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedCountThisCombat
	{
		get => _countThisCombat;
		set
		{
			_countThisCombat = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount => !IsCanonical ? _countThisCombat : 0;

	public override Task BeforeCombatStart()
	{
		_countThisCombat = 0;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		if (Owner != null && _countThisCombat > 0)
		{
			HextechGoldRewardHelper.AddFixedExtraGoldReward(room, Owner, _countThisCombat);
		}

		_countThisCombat = 0;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (target.Side != CombatSide.Enemy || result.TotalDamage <= 0 || !IsDamageFromOwner(dealer, cardSource))
		{
			return Task.CompletedTask;
		}

		_countThisCombat += DynamicVars["CountPerHit"].IntValue;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

}
