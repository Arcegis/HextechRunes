namespace HextechRunes;

public sealed class RekindleRune : HextechRelicBase
{
	private int _exhaustedCardsThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedExhaustedCardsThisCombat
	{
		get => _exhaustedCardsThisCombat;
		set
		{
			_exhaustedCardsThisCombat = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount => !IsCanonical ? Math.Max(0, DynamicVars.Cards.IntValue - _exhaustedCardsThisCombat) : 0;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2),
		new EnergyVar(1)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsIroncladPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		ResetCounter();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetCounter();
		return Task.CompletedTask;
	}

	public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		if (!IsOwnedCard(card) || Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		_exhaustedCardsThisCombat++;
		int energyToGain = 0;
		while (_exhaustedCardsThisCombat >= DynamicVars.Cards.IntValue)
		{
			_exhaustedCardsThisCombat -= DynamicVars.Cards.IntValue;
			energyToGain++;
		}

		InvokeDisplayAmountChanged();
		if (energyToGain <= 0)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue * energyToGain, Owner);
	}

	private void ResetCounter()
	{
		_exhaustedCardsThisCombat = 0;
		InvokeDisplayAmountChanged();
	}
}
