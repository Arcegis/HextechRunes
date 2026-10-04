using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace HextechRunesSponsorPack;

public sealed class ArcaneForge : SelectableEnchantmentForgeBase
{
	private const string CloneRestSiteOptionId = "CLONE";

	private static readonly IReadOnlyList<SelectableEnchantmentOption> Options =
	[
		SelectableEnchantmentOption.For<Clone>(() => ModelDb.Relic<ArcaneCloneChoiceRelic>()),
		SelectableEnchantmentOption.For<SoulsPower>(() => ModelDb.Relic<ArcaneSoulsPowerChoiceRelic>()),
		SelectableEnchantmentOption.For<RoyallyApproved>(() => ModelDb.Relic<ArcaneRoyallyApprovedChoiceRelic>())
	];

	private protected override IReadOnlyList<SelectableEnchantmentOption> EnchantmentOptions => Options;

	private protected override string ChoiceContextName => "arcane-forge-enchantment-choice";

	public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
	{
		if (player != Owner || options.Any(static option => option.OptionId == CloneRestSiteOptionId))
		{
			return false;
		}

		if (!Owner.Deck.Cards.Any(HasCloneEnchantment))
		{
			return false;
		}

		options.Add(new CloneRestSiteOption(player));
		return true;
	}

	private static bool HasCloneEnchantment(CardModel card)
	{
		// 原版语义:card.Enchantment 就是这张牌的附魔。装了多重附魔类模组时由它们的 IL 重写器接管这种写法。
		return card.Enchantment is Clone;
	}
}

// 稀有度与棱彩锻造器图标三件套由 PrismaticForgeChoiceRelic 提供。
public abstract class ArcaneEnchantmentChoiceRelic<TEnchantment> : PrismaticForgeChoiceRelic
	where TEnchantment : EnchantmentModel
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. HoverTipFactory.FromEnchantment<TEnchantment>()
	];
}

public sealed class ArcaneCloneChoiceRelic : ArcaneEnchantmentChoiceRelic<Clone>
{
}

public sealed class ArcaneSoulsPowerChoiceRelic : ArcaneEnchantmentChoiceRelic<SoulsPower>
{
}

public sealed class ArcaneRoyallyApprovedChoiceRelic : ArcaneEnchantmentChoiceRelic<RoyallyApproved>
{
}
