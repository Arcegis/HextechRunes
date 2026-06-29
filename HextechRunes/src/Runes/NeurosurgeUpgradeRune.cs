using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace HextechRunes;

// 升级：精神过载(仅骨妹) —— 把 Neurosurge 每回合施加的灾厄(DoomPower)从「自身」改为「全体敌人」。
// 真正的重定向在 HextechNeurosurgeHooks(Harmony 改 NeurosurgePower.AfterSideTurnStart)。本类仅负责门控与 hover。
public sealed class NeurosurgeUpgradeRune : CardUpgradeRuneBase<Neurosurge>
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<Neurosurge>(),
		HoverTipFactory.FromPower<DoomPower>()
	];

	protected override bool IsAvailableForCharacter(Player player) => IsNecrobinderPlayer(player);
}
