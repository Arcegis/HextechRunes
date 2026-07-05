using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunes;

/// <summary>选择界面卡片底部的小字提示(如升级类海克斯的"如果你没有XX,则获得1张XX")。</summary>
internal interface IHextechSelectionFooterProvider
{
	string? GetSelectionFooterText();
}

// 0.8.4 起「升级：XX」系不再要求牌组已有目标卡才进池(移除刷新门槛);
// 改为获得时补卡:若牌组内没有目标卡(含变体,见 DeckContainsRequiredCard),向牌组加入 1 张 TCard。
// 补卡说明不进主描述(阅读负担),以选择界面 footer 小字呈现(见 IHextechSelectionFooterProvider)。
// 覆盖 AfterObtained 的子类(如 UndyingUpgradeRune)必须先 await base.AfterObtained() 保证补卡先行。
public abstract class CardUpgradeRuneBase<TCard> : HextechRelicBase, IHextechSelectionFooterProvider
	where TCard : CardModel
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromCard<TCard>()
	];

	public sealed override bool IsAvailableForPlayer(Player player)
	{
		return IsAvailableForCharacter(player);
	}

	/// <summary>
	/// 获得时是否无需补卡:默认=牌组已有 TCard。支持变体卡的子类(Bash||Break 等)override 后,
	/// 牌组已有任一变体也不补——补卡目的只是保证符文不空转。
	/// </summary>
	protected virtual bool DeckContainsRequiredCard(Player player)
	{
		return DeckContains<TCard>(player);
	}

	public override Task AfterObtained()
	{
		return Owner != null && !DeckContainsRequiredCard(Owner)
			? AddCardCopiesToDeckOrHand<TCard>(1)
			: Task.CompletedTask;
	}

	protected abstract bool IsAvailableForCharacter(Player player);

	// virtual:变体类子类(Bash||Break 等)可覆写以精确表述。
	public virtual string? GetSelectionFooterText()
	{
		try
		{
			// 占位符必须走 LocString 变量机制:裸 {0} 会被 SmartFormat 当变量解析,
			// 找不到时把内部字典 ToString 吐进文本。
			LocString footer = new("relics", "hextechUpgradeRune.selectionFooter");
			footer.Add("CardName", ModelDb.Card<TCard>().Title);
			return footer.GetFormattedText();
		}
		catch
		{
			return null;
		}
	}
}
