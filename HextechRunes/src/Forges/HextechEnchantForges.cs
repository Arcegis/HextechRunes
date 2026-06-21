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
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. HoverTipFactory.FromEnchantment<TEnchantment>()
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		EnchantmentModel enchantment = ModelDb.Enchantment<TEnchantment>().ToMutable();
		CardModel? selected = (await CardSelectCmd.FromDeckForEnchantment(
			Owner,
			enchantment,
			1,
			new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1))).FirstOrDefault();
		if (selected == null)
		{
			return;
		}

		Flash();
		CardCmd.Enchant(enchantment, selected, 1);
	}
}

public sealed class GlamForge : EnchantmentForgeBase<Glam>
{
}

public sealed class SpiralForge : EnchantmentForgeBase<UniversalSpiral>
{
}
