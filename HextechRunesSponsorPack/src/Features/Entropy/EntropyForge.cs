using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunesSponsorPack;

public sealed class EntropyForge : SelectableEnchantmentForgeBase
{
	private static readonly IReadOnlyList<SelectableEnchantmentOption> Options =
	[
		SelectableEnchantmentOption.For<EntropyIncrease>(() => ModelDb.Relic<EntropyIncreaseChoiceRelic>()),
		SelectableEnchantmentOption.For<EntropyDecrease>(() => ModelDb.Relic<EntropyDecreaseChoiceRelic>())
	];

	private protected override IReadOnlyList<SelectableEnchantmentOption> EnchantmentOptions => Options;

	private protected override string ChoiceContextName => "entropy-forge-enchantment-choice";
}

public sealed class EntropyIncreaseChoiceRelic : GoldForgeChoiceRelic
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. HoverTipFactory.FromEnchantment<EntropyIncrease>()
	];
}

public sealed class EntropyDecreaseChoiceRelic : GoldForgeChoiceRelic
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. HoverTipFactory.FromEnchantment<EntropyDecrease>()
	];
}
