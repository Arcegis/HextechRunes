namespace HextechRunes;

public sealed class GalacticGiftRune : HextechRelicBase
{
	private int _starsSpentThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedStarsSpentThisCombat
	{
		get => _starsSpentThisCombat;
		set
		{
			_starsSpentThisCombat = Math.Max(0, value);
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

			int starsNeeded = DynamicVars["StarsSpent"].IntValue;
			int remainder = _starsSpentThisCombat % starsNeeded;
			return remainder == 0 ? starsNeeded : starsNeeded - remainder;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new StarsVar("StarsSpent", 3),
		new StarsVar(1)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsRegentPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		ResetStarsSpent();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		ResetStarsSpent();
		return Task.CompletedTask;
	}

	public override Task AfterStarsSpent(int amount, Player spender)
	{
		if (spender != Owner || Owner == null || Owner.Creature.IsDead || amount <= 0)
		{
			return Task.CompletedTask;
		}

		int starsNeeded = DynamicVars["StarsSpent"].IntValue;
		_starsSpentThisCombat += amount;
		int rewards = _starsSpentThisCombat / starsNeeded;
		_starsSpentThisCombat %= starsNeeded;
		InvokeDisplayAmountChanged();
		if (rewards <= 0)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PlayerCmd.GainStars(rewards * DynamicVars.Stars.BaseValue, Owner);
	}

	private void ResetStarsSpent()
	{
		_starsSpentThisCombat = 0;
		InvokeDisplayAmountChanged();
	}
}
