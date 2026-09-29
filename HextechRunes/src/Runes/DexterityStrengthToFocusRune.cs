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

	protected override bool ShouldConvert(PowerModel power)
	{
		return IsDefectOwner && power is DexterityPower or StrengthPower;
	}

	protected override Task ApplyConvertedPower(Creature owner, decimal amount, Creature? applier, CardModel? cardSource)
	{
		return PowerCmd.Apply<FocusPower>(owner, amount, applier, cardSource);
	}

	protected override Task RevertOriginalPower(Creature owner, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		return power switch
		{
			DexterityPower => PowerCmd.Apply<DexterityPower>(owner, -amount, applier, cardSource),
			StrengthPower => PowerCmd.Apply<StrengthPower>(owner, -amount, applier, cardSource),
			_ => Task.CompletedTask
		};
	}
}
