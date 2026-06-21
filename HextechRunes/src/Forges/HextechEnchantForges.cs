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
	private const int EnchantmentAmount = 1;

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
			EnchantmentAmount,
			new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, EnchantmentAmount));
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

public sealed class SpiralForge : EnchantmentForgeBase<UniversalSpiral>
{
}
