namespace HextechRunes;

/// <summary>百分比生命锻造器:记录自己累计发放过的最大生命,让百分比按系数加算而非复利。</summary>
public interface IHextechPercentHpForge
{
	int GrantedPercentHp { get; }
}

public abstract class HextechForgeBase : HextechRelicBase
{
	/// <summary>
	/// 百分比生命锻造器的计算基数 = 当前最大生命 - 所有百分比生命锻造器已发放的量,
	/// 即"非百分比来源"的最大生命。每次按基数取百分比,多次获取为系数加算(1+p1+p2...)而非复利。
	/// </summary>
	protected static decimal GetPercentHpBaseline(Player owner)
	{
		int granted = owner.Relics
			.OfType<IHextechPercentHpForge>()
			.Sum(static forge => forge.GrantedPercentHp);
		return Math.Max(1m, owner.Creature.MaxHp - granted);
	}

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
		if (StackAmount <= 1)
		{
			return value;
		}

		return (decimal)Math.Pow((double)value, StackAmount);
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
