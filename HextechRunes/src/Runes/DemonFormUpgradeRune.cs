using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

// 0.8.4 重做:回合结束时,手牌内有DemonForm则自动打出;获得时补卡(基类)。
public sealed class DemonFormUpgradeRune : PlayFromHandOnTurnEndRuneBase<DemonForm>
{
	protected override bool IsAvailableForCharacter(Player player) => IsIroncladPlayer(player);
}
