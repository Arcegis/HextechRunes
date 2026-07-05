namespace HextechRunes;

public sealed class GiantSlayerRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2),
		new DynamicVar("HpGap", 6m),
		new DynamicVar("DamagePerStepPercent", 0.01m),
		new DynamicVar("MaxBonusPercent", 0.5m),
		new DynamicVar("Scale", 0.65m)
	];

	internal float BodyScaleDelta => (float)DynamicVars["Scale"].BaseValue - 1f;

	public override Task AfterObtained()
	{
		HextechPlayerBodyScaleHelper.Update(Owner);
		return Task.CompletedTask;
	}

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		HextechPlayerBodyScaleHelper.Update(Owner);
		return Task.CompletedTask;
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (player != Owner)
		{
			return count;
		}

		return count + DynamicVars.Cards.BaseValue;
	}

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (Owner == null || target?.Side != CombatSide.Enemy || !IsDamageFromOwner(dealer, cardSource))
		{
			return 1m;
		}

		int hpGap = target.MaxHp - Owner.Creature.MaxHp;
		if (hpGap <= 0)
		{
			return 1m;
		}

		int steps = hpGap / DynamicVars["HpGap"].IntValue;
		decimal bonus = Math.Min(steps * DynamicVars["DamagePerStepPercent"].BaseValue, DynamicVars["MaxBonusPercent"].BaseValue);
		return 1m + bonus;
	}
}
