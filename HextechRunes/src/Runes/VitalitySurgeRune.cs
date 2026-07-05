namespace HextechRunes;

/// <summary>生机迸发:每 50 点最大生命值每回合多抽 1 张牌;每 100 点每回合额外获得 1 点能量。</summary>
public sealed class VitalitySurgeRune : HextechRelicBase
{
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("HpPerCard", 50m),
		new DynamicVar("HpPerEnergy", 100m)
	];

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (player != Owner)
		{
			return count;
		}

		return count + Math.Floor(player.Creature.MaxHp / DynamicVars["HpPerCard"].BaseValue);
	}

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		if (player != Owner)
		{
			return amount;
		}

		return amount + Math.Floor(player.Creature.MaxHp / DynamicVars["HpPerEnergy"].BaseValue);
	}
}
