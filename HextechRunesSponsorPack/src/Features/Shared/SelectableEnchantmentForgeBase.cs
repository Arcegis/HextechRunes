using HextechRunes;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace HextechRunesSponsorPack;

/// <summary>
/// 「先从牌组选 1 张牌,再在几种附魔的选项遗物里挑一个,附 1 层」的锻造器(奥术、熵)。
/// 与按牌型直接决定附魔的 <see cref="ConditionalEnchantmentForgeBase"/> 并列。
/// </summary>
public abstract class SelectableEnchantmentForgeBase : HextechForgeBase
{
	// 选牌数量与附魔层数是两件事,不要混用:两者目前都恰好是 1。
	private const int CardSelectionCount = 1;
	private const int EnchantmentAmount = 1;

	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. EnchantmentOptions.SelectMany(static option => option.CreateHoverTips())
	];

	private protected abstract IReadOnlyList<SelectableEnchantmentOption> EnchantmentOptions { get; }

	// 选项遗物选择的上下文前缀(联机同步与日志用),形如 "arcane-forge-enchantment-choice"。
	private protected abstract string ChoiceContextName { get; }

	public override async Task AfterObtained()
	{
		IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromDeckGeneric(
			Owner,
			new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, CardSelectionCount),
			CanEnchantWithAnyOption);
		CardModel? selectedCard = selectedCards.FirstOrDefault();
		if (selectedCard == null)
		{
			return;
		}

		EnchantmentModel? selectedEnchantment = await SelectEnchantment(
			Owner,
			selectedCard,
			$"{ChoiceContextName} card={(selectedCard.CanonicalInstance?.Id ?? selectedCard.Id).Entry}");
		if (selectedEnchantment == null)
		{
			return;
		}

		Flash();
		CardCmd.Enchant(selectedEnchantment.ToMutable(), selectedCard, EnchantmentAmount);
		CardCmd.Preview(selectedCard);
	}

	private bool CanEnchantWithAnyOption(CardModel card)
	{
		return EnchantmentOptions.Any(option => option.CreateCanonical().CanEnchant(card));
	}

	private async Task<EnchantmentModel?> SelectEnchantment(Player owner, CardModel card, string choiceContext)
	{
		IReadOnlyList<SelectableEnchantmentOption> applicable = EnchantmentOptions
			.Where(option => option.CreateCanonical().CanEnchant(card))
			.ToArray();
		if (applicable.Count == 0)
		{
			return null;
		}

		RelicModel[] choiceRelics = applicable
			.Select(static option => option.CreateChoiceRelic())
			.ToArray();
		// 本地选择界面与联机远端分支都返回 choiceRelics 里的同一个实例,按引用找回下标即可。
		RelicModel? selected = await HextechRunesApi.SelectRelicOption(owner, choiceRelics, choiceContext);
		int selectedIndex = selected == null ? -1 : Array.IndexOf(choiceRelics, selected);
		return selectedIndex >= 0 ? applicable[selectedIndex].CreateCanonical() : null;
	}
}

internal sealed record SelectableEnchantmentOption(
	Func<RelicModel> CreateChoiceRelic,
	Func<EnchantmentModel> CreateCanonical,
	Func<IEnumerable<IHoverTip>> CreateHoverTips)
{
	internal static SelectableEnchantmentOption For<TEnchantment>(Func<RelicModel> createChoiceRelic)
		where TEnchantment : EnchantmentModel
	{
		return new SelectableEnchantmentOption(
			createChoiceRelic,
			() => ModelDb.Enchantment<TEnchantment>(),
			() => HoverTipFactory.FromEnchantment<TEnchantment>());
	}
}
