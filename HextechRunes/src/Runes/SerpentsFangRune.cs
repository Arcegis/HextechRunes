namespace HextechRunes;

public sealed class SerpentsFangRune : HextechRelicBase
{
	// 0.8.4 数据(83.8% 胜率 vs 棱彩均值 72.2%)证实"等量中毒"=伤害无条件翻倍,超模;改为固定层数。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("PoisonPerHit", 4m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<PoisonPower>()
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsSilentPlayer(player);
	}

	public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (Owner == null
			|| Owner.Creature.IsDead
			|| target.Side != CombatSide.Enemy
			|| result.UnblockedDamage <= 0
			|| HextechCombatHooks.IsResolvingOutbreakPowerPoisonResponse
			|| HextechCombatHooks.IsResolvingSleightOfFleshPowerDebuffResponse
			|| !IsDamageFromOwner(dealer, cardSource))
		{
			return;
		}

		Flash([target]);
		await PowerCmd.Apply<PoisonPower>(target, DynamicVars["PoisonPerHit"].BaseValue, Owner.Creature, null);
	}
}
