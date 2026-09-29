namespace HextechRunes;

public sealed class FirstAidKitRune : HextechRelicBase, IHextechHealingMultiplierProvider
{
	// 格挡与治疗共用；文案写的是字面值，改数值要同步九语言。
	private const decimal SustainMultiplier = 1.25m;

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		return target == Owner?.Creature ? SustainMultiplier : 1m;
	}

	decimal IHextechHealingMultiplierProvider.ModifyHealingMultiplicative(Player player, Creature creature, decimal amount)
	{
		return IsFirstOwnedInstance(player) ? SustainMultiplier : 1m;
	}
}
