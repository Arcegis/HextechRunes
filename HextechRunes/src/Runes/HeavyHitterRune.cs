namespace HextechRunes;

public sealed class HeavyHitterRune : HextechRelicBase
{
	// 每 6 点最大生命值 +1% 伤害。
	private const decimal MaxHpPerDamagePercent = 6m;

	public override decimal ModifyDamageMultiplicativeCompat(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (Owner == null || !IsDamageFromOwnerToEnemyOrPreview(target, dealer, cardSource))
		{
			return 1m;
		}

		// 战斗外（如左上角“伤害系数”头像悬浮）没有战斗用的 Creature，回退到持久的 Osty；
		// 否则 Owner.Creature 为 null 会抛 NPE 并被系数助手的 catch 静默吞掉，导致加成不显示。
		Creature? source = Owner.Creature ?? Owner.Osty;
		if (source == null)
		{
			return 1m;
		}

		return 1m + Math.Floor(source.MaxHp / MaxHpPerDamagePercent) / 100m;
	}
}
