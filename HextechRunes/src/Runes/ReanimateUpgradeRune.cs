namespace HextechRunes;

public sealed class ReanimateUpgradeRune : CardUpgradeRuneBase<Reanimate>
{
	private const decimal BaseCostReduction = 1m;

	private int _deathsThisCombat;

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedDeathsThisCombat
	{
		get => _deathsThisCombat;
		set => _deathsThisCombat = Math.Max(0, value);
	}

	protected override bool IsAvailableForCharacter(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override Task BeforeCombatStart()
	{
		_deathsThisCombat = 0;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_deathsThisCombat = 0;
		return Task.CompletedTask;
	}

	public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
	{
		// 原版忧郁只排除被阻止的死亡；爪牙和小手回调时可能已经脱离 CombatState。
		if (!wasRemovalPrevented)
		{
			_deathsThisCombat++;
			Flash();
			RefreshReanimateCostsInHand();
		}

		return Task.CompletedTask;
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (card.Owner != Owner
			|| card is not Reanimate
			|| card.EnergyCost.CostsX)
		{
			return false;
		}

		decimal reducedCost = Math.Max(0m, originalCost - BaseCostReduction - _deathsThisCombat);
		if (reducedCost == originalCost)
		{
			return false;
		}

		modifiedCost = reducedCost;
		return true;
	}

	private void RefreshReanimateCostsInHand()
	{
		foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards)
		{
			if (card is Reanimate)
			{
				try
				{
					card.InvokeEnergyCostChanged();
				}
				catch (Exception ex)
				{
					HextechLog.Warn("ReanimateUpgrade", $"Cost visual refresh failed: {ex.Message}");
				}
			}
		}
	}
}
