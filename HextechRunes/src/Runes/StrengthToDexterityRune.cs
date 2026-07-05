namespace HextechRunes;

public sealed class StrengthToDexterityRune : AttributeConversionRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<DexterityPower>(1m)
	];

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is not CombatRoom || Owner == null)
		{
			return Task.CompletedTask;
		}

		return PowerCmd.Apply<DexterityPower>(Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, null);
	}

	protected override bool ShouldConvert(PowerModel canonicalPower)
	{
		return canonicalPower is StrengthPower;
	}

	protected override bool ShouldConvertAppliedPower(PowerModel power)
	{
		return power is StrengthPower;
	}

	protected override Task ApplyConvertedPower(decimal amount, Creature? applier, CardModel? cardSource)
	{
		return PowerCmd.Apply<DexterityPower>(Owner!.Creature, amount, applier, cardSource);
	}

	protected override Task RevertOriginalPower(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		return PowerCmd.Apply<StrengthPower>(Owner!.Creature, -amount, applier, cardSource);
	}
}
