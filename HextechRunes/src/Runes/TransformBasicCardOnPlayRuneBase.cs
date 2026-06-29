using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace HextechRunes;

// 「打出某类基础卡后,把它在本场战斗中变化为另一张卡」的共用基类。
// 时机:卡打出后会从 PileType.Play 移入弃牌堆。不能在 AfterCardPlayed/Late 里转化(那时卡仍在打出区、
// 之后还要被移堆,中途转化会破坏移堆 → 卡牌卡在空中)。改在 AfterCardChangedPilesLate:卡刚从 Play 落定到
// 弃牌堆那一刻再用 CardPreviewStyle.None 静默转化 —— 即时生效、不卡牌,与「从弃牌堆选牌入手」也相性正常。
// oldPileType == Play 恰好只命中「打出」的卡(手牌弃掉的是 Hand→弃牌堆,不会误伤);Finesse/亮剑非基础卡,
// 转化产生的新卡不会再次触发,无循环。
public abstract class TransformBasicCardOnPlayRuneBase<TReplacement> : HextechRelicBase
	where TReplacement : CardModel
{
	protected abstract bool ShouldTransform(CardModel card);

	public override async Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
	{
		if (Owner == null
			|| card.Owner != Owner
			|| oldPileType != PileType.Play
			|| card.Pile == null
			|| !ShouldTransform(card)
			|| card.CombatState is not { } combatState)
		{
			return;
		}

		Flash();
		CardModel replacement = combatState.CreateCard<TReplacement>(Owner);
		CardTransformUpgradeHelper.PreserveUpgradeLevel(card, replacement);
		await CardCmd.Transform(card, replacement, CardPreviewStyle.None);
	}
}
