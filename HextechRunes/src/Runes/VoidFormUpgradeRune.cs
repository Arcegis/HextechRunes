using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Cards;

namespace HextechRunes;

// 0.8.4 重做:回合结束时,手牌内有VoidForm则自动打出;获得时补卡(基类)。
public sealed class VoidFormUpgradeRune : AutoPlayFormsAtCombatStartRuneBase<VoidForm>
{
	protected override bool IsAvailableForCharacter(Player player) => IsRegentPlayer(player);
}
