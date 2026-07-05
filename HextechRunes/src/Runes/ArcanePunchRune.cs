namespace HextechRunes;

public sealed class ArcanePunchRune : HextechRelicBase
{
	private int _attacksPlayedThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedAttacksPlayedThisCombat
	{
		get => IsNetworkMultiplayer() ? 0 : GetAttacksPlayedThisCombat();
		set
		{
			_attacksPlayedThisCombat = Math.Max(0, value);
			InvokeDisplayAmountChanged();
		}
	}

	public override bool ShowCounter => CombatManager.Instance?.IsInProgress == true && !IsCanonical;

	public override int DisplayAmount
	{
		get
		{
			if (IsCanonical)
			{
				return 0;
			}

			int remainder = GetAttacksPlayedThisCombat() % 2;
			return remainder == 0 ? 2 : 1;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("AttacksPerEnergy", 2m),
		new EnergyVar(1)
	];

	public override Task BeforeCombatStart()
	{
		_attacksPlayedThisCombat = 0;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_attacksPlayedThisCombat = 0;
		InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!IsOwnedAttack(cardPlay.Card))
		{
			return;
		}

		if (ShouldUseNetworkCombatHistory())
		{
			await ResolveAttackProgressFromHistory();
			return;
		}

		int attacksPlayed = _attacksPlayedThisCombat + 1;
		_attacksPlayedThisCombat = attacksPlayed;
		InvokeDisplayAmountChanged();
		if (attacksPlayed % 2 != 0)
		{
			return;
		}

		await GainEnergyForAttackThreshold();
	}

	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (ShouldUseNetworkCombatHistory() && IsOwnedAttack(cardPlay.Card))
		{
			await ResolveAttackProgressFromHistory();
		}
	}

	private async Task ResolveAttackProgressFromHistory()
	{
		int attacksPlayed = CountOwnedAttackCardsPlayedFromHistory(firstInSeriesOnly: false, includeAutoPlay: true);
		int previousAttacksPlayed = _attacksPlayedThisCombat;
		if (attacksPlayed <= previousAttacksPlayed)
		{
			return;
		}

		_attacksPlayedThisCombat = attacksPlayed;
		InvokeDisplayAmountChanged();
		int energyTriggers = attacksPlayed / 2 - previousAttacksPlayed / 2;
		for (int i = 0; i < energyTriggers; i++)
		{
			await GainEnergyForAttackThreshold();
		}
	}

	private async Task GainEnergyForAttackThreshold()
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainEnergy(1m, Owner);
	}

	private int GetAttacksPlayedThisCombat()
	{
		return ShouldUseNetworkCombatHistory()
			? CountOwnedAttackCardsPlayedFromHistory(firstInSeriesOnly: false, includeAutoPlay: true)
			: _attacksPlayedThisCombat;
	}
}
