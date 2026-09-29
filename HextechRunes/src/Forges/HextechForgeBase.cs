namespace HextechRunes;

public abstract class HextechForgeBase : HextechRelicBase
{
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public int SavedStackCount
	{
		get => StackCount;
		set
		{
			int target = Math.Max(1, value);
			while (StackCount < target)
			{
				IncrementStackCount();
			}

			InvokeDisplayAmountChanged();
		}
	}

	public override bool IsStackable => true;

	public override bool ShowCounter => true;

	public override int DisplayAmount => !IsCanonical ? StackCount : 0;

	protected int StackAmount => Math.Max(1, StackCount);

	protected decimal StackMultiplier => StackAmount;

	protected decimal Stacked(decimal value)
	{
		return value * StackMultiplier;
	}

	protected decimal StackedMultiplier(decimal value)
	{
		return 1m + (value - 1m) * StackAmount;
	}

	// 锻造器最常见的效果：战斗开始时按叠层给存活的持有者施加一个 Power。
	protected Task ApplyStackedPowerAtCombatStart<TPower>(decimal amountPerStack)
		where TPower : PowerModel
	{
		if (Owner == null || Owner.Creature.IsDead)
		{
			return Task.CompletedTask;
		}

		Flash();
		return PowerCmd.Apply<TPower>(Owner.Creature, Stacked(amountPerStack), Owner.Creature, null);
	}

	public void AddForgeStack(bool flash = true)
	{
		IncrementStackCount();
		InvokeDisplayAmountChanged();
		if (flash)
		{
			Flash();
		}
	}
}
