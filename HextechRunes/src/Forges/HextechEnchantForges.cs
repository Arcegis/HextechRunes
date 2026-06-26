using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace HextechRunes;

public abstract class EnchantmentForgeBase<TEnchantment> : HextechForgeBase
	where TEnchantment : EnchantmentModel
{
	protected virtual int EnchantmentAmount => 1;

	// 选牌张数与附魔层数解耦:MomentumForge 要「选 1 张牌附魔动量3」,层数=3 但只选 1 张。
	protected virtual int EnchantmentCardCount => 1;

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. HoverTipFactory.FromEnchantment<TEnchantment>(EnchantmentAmount)
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		EnchantmentModel canonicalEnchantment = ModelDb.Enchantment<TEnchantment>();
		IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromDeckForEnchantment(
			Owner,
			canonicalEnchantment,
			EnchantmentCardCount,
			new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, EnchantmentCardCount));
		foreach (CardModel selected in selectedCards)
		{
			Flash();
			CardCmd.Enchant(canonicalEnchantment.ToMutable(), selected, EnchantmentAmount);
			CardCmd.Preview(selected);
		}
	}
}

public sealed class GlamForge : EnchantmentForgeBase<Glam>
{
}

public sealed class SwiftForge : EnchantmentForgeBase<Swift>
{
}

public sealed class MomentumForge : EnchantmentForgeBase<Momentum>
{
	protected override int EnchantmentAmount => 3;
}

public sealed class EmbersForge : EnchantmentForgeBase<TezcatarasEmber>
{
}

public sealed class SpiralForge : EnchantmentForgeBase<UniversalSpiral>
{
}
