namespace HextechRunes;

public sealed class DexterityStrengthToFocusRune : AttributeConversionRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<FocusPower>(1m)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is not CombatRoom || Owner == null || !IsDefectOwner)
		{
			return Task.CompletedTask;
		}

		return PowerCmd.Apply<FocusPower>(Owner.Creature, DynamicVars["FocusPower"].BaseValue, Owner.Creature, null);
	}

	protected override bool ShouldConvert(PowerModel canonicalPower)
	{
		return IsDefectOwner && (canonicalPower is DexterityPower || canonicalPower is StrengthPower);
	}

	protected override bool ShouldConvertAppliedPower(PowerModel power)
	{
		return IsDefectOwner && (power is DexterityPower || power is StrengthPower);
	}

	protected override Task ApplyConvertedPower(decimal amount, Creature? applier, CardModel? cardSource)
	{
		return PowerCmd.Apply<FocusPower>(Owner!.Creature, amount, applier, cardSource);
	}

	protected override Task RevertOriginalPower(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (Owner == null)
		{
			return Task.CompletedTask;
		}

		if (power is DexterityPower)
		{
			return PowerCmd.Apply<DexterityPower>(Owner.Creature, -amount, applier, cardSource);
		}

		if (power is StrengthPower)
		{
			return PowerCmd.Apply<StrengthPower>(Owner.Creature, -amount, applier, cardSource);
		}

		return Task.CompletedTask;
	}
}
