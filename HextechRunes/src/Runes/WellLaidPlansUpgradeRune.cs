namespace HextechRunes;

// 升级：计划妥当(仅猎人) —— 计划妥当(WellLaidPlans)回合结束保留手牌时,可保留任意张(上限改为手牌数)。
// 真正放开上限在 HextechWellLaidPlansHooks(Harmony 改 WellLaidPlansPower.BeforeFlushLate)。本类仅负责门控与 hover。
public sealed class WellLaidPlansUpgradeRune : CardUpgradeRuneBase<WellLaidPlans>
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<WellLaidPlans>()
	];

	protected override bool IsAvailableForCharacter(Player player) => IsSilentPlayer(player);

#if STS2_109_OR_NEWER
	// 0.109 原版计划妥当重做为「整手牌全保留」,power 上不再有选牌挂点;选牌逻辑搬到 rune 自身
	// (配合 HextechWellLaidPlansHooks 把 power.ShouldFlush 拉回 true),维持「任选保留、其余弃掉」。
	public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
	{
		if (Owner == null || player != Owner || Owner.Creature == null || Owner.Creature.IsDead)
		{
			return;
		}

		if (Owner.Creature.GetPower<WellLaidPlansPower>() is not WellLaidPlansPower power)
		{
			return;
		}

		await HextechWellLaidPlansHooks.UnlimitedRetain(power, choiceContext, player);
	}
#endif
}
