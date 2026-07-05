namespace HextechRunes;

public sealed class BrokenGoldenCrownRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("RewardOptionsLost", 2m),
		new EnergyVar(1)
	];

	public override bool TryModifyCardRewardOptionsLate(
		Player player,
		List<CardCreationResult> cardRewardOptions,
		CardCreationOptions creationOptions)
	{
		if (player != Owner
			|| creationOptions.Source != CardCreationSource.Encounter
			|| cardRewardOptions.Count <= 1)
		{
			return false;
		}

		int optionsToRemove = Math.Min(DynamicVars["RewardOptionsLost"].IntValue, cardRewardOptions.Count - 1);
		for (int i = 0; i < optionsToRemove; i++)
		{
			cardRewardOptions.RemoveAt(cardRewardOptions.Count - 1);
		}

		Flash();
		return true;
	}

	public override Task AfterEnergyResetLate(Player player)
	{
		if (player != Owner || Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		return PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, player);
	}
}
