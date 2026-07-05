namespace HextechRunes;

public sealed class DrawYourSwordRune : AttributeConversionRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<FocusPower>(1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromOrb<LightningOrb>(),
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>(),
		HoverTipFactory.FromPower<FocusPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsDefectPlayer(player);
	}

	internal bool ShouldConvertChanneledOrb(Player player)
	{
		return player == Owner
			&& Owner != null
			&& IsDefectOwner
			&& !Owner.Creature.IsDead
			&& CombatManager.Instance?.IsOverOrEnding != true
			&& player.PlayerCombatState != null;
	}

	internal async Task ConvertChanneledOrbToFocus(PlayerChoiceContext choiceContext, OrbModel orb, Player player)
	{
		if (!ShouldConvertChanneledOrb(player))
		{
			return;
		}

		choiceContext.PushModel(orb);
		try
		{
			Flash();
			await PowerCmd.Apply<FocusPower>(Owner!.Creature, DynamicVars["FocusPower"].BaseValue, Owner.Creature, null);
		}
		finally
		{
			choiceContext.PopModel(orb);
		}
	}

	protected override bool ShouldConvert(PowerModel canonicalPower)
	{
		return IsDefectOwner && !HasConflictingFocusConverter && canonicalPower is FocusPower;
	}

	protected override bool ShouldConvertAppliedPower(PowerModel power)
	{
		return IsDefectOwner && !HasConflictingFocusConverter && power is FocusPower;
	}

	protected override async Task ApplyConvertedPower(decimal amount, Creature? applier, CardModel? cardSource)
	{
		await PowerCmd.Apply<StrengthPower>(Owner!.Creature, amount, applier, cardSource);
		await PowerCmd.Apply<DexterityPower>(Owner.Creature, amount, applier, cardSource);
	}

	protected override Task RevertOriginalPower(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		return PowerCmd.Apply<FocusPower>(Owner!.Creature, -amount, applier, cardSource);
	}

	private bool HasConflictingFocusConverter => Owner?.GetRelic<DexterityStrengthToFocusRune>() != null;
}
