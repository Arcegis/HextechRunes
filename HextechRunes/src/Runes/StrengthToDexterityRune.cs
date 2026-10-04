namespace HextechRunes;

public sealed class StrengthToDexterityRune : AttributeConversionRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<DexterityPower>(1m)
	];

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is not CombatRoom)
		{
			return Task.CompletedTask;
		}

		return PowerCmd.Apply<DexterityPower>(Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, null);
	}

	protected override bool ShouldConvert(PowerModel power)
	{
		return power is StrengthPower;
	}

	protected override Task ApplyConvertedPower(Creature owner, decimal amount, Creature? applier, CardModel? cardSource)
	{
		return PowerCmd.Apply<DexterityPower>(owner, amount, applier, cardSource);
	}

	protected override Task RevertOriginalPower(Creature owner, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		return PowerCmd.Apply<StrengthPower>(owner, -amount, applier, cardSource);
	}
}
