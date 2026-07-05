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
}
