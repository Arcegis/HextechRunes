using HextechRunes;
using System.Reflection;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Relics;

namespace HextechRunesSponsorPack;

public sealed class BasicForge : HextechForgeBase
{
	public override bool HasUponPickupEffect => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. HoverTipFactory.FromRelic<PaelsClaw>(),
		.. HoverTipFactory.FromRelic<NutritiousSoup>()
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		IReadOnlyList<RelicModel> choiceRelics = CreateChoiceRelics();
		RelicModel? selected = await HextechRunesApi.SelectRelicOption(Owner, choiceRelics, "basic-forge-relic-choice");
		if (selected == null)
		{
			return;
		}

		Flash();
		await RelicCmd.Obtain(selected.ToMutable(), Owner);
	}

	private static IReadOnlyList<RelicModel> CreateChoiceRelics()
	{
		return
		[
			ModelDb.Relic<PaelsClaw>(),
			ModelDb.Relic<NutritiousSoup>()
		];
	}
}

public sealed class ArcaneForge : HextechForgeBase
{
	private const int SelectionCount = 1;

	private static readonly IReadOnlyList<ArcaneEnchantmentOption> EnchantmentOptions =
	[
		ArcaneEnchantmentOption.For<Clone>(() => ModelDb.Relic<ArcaneCloneChoiceRelic>()),
		ArcaneEnchantmentOption.For<SoulsPower>(() => ModelDb.Relic<ArcaneSoulsPowerChoiceRelic>()),
		ArcaneEnchantmentOption.For<RoyallyApproved>(() => ModelDb.Relic<ArcaneRoyallyApprovedChoiceRelic>())
	];

	public override bool HasUponPickupEffect => true;

	public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
	{
		if (Owner == null || player != Owner || options.Any(static option => option.OptionId == "CLONE"))
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

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		.. EnchantmentOptions.SelectMany(static option => option.CreateHoverTips())
	];

	public override async Task AfterObtained()
	{
		if (Owner == null)
		{
			return;
		}

		IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromDeckGeneric(
			Owner,
			new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, SelectionCount),
			CanEnchantWithAnyOption);
		foreach (CardModel selectedCard in selectedCards)
		{
			IReadOnlyList<ArcaneEnchantmentOption> options = GetApplicableOptions(selectedCard);
			if (options.Count == 0)
			{
				continue;
			}

			IReadOnlyList<RelicModel> choiceRelics = options.Select(static option => option.CreateChoiceRelic()).ToArray();
			RelicModel? selectedRelic = await HextechRunesApi.SelectRelicOption(Owner, choiceRelics, $"arcane-forge-enchantment-choice card={(selectedCard.CanonicalInstance?.Id ?? selectedCard.Id).Entry}");
			if (selectedRelic == null)
			{
				continue;
			}

			int selectedIndex = IndexOfRelic(choiceRelics, selectedRelic);
			if (selectedIndex < 0 || selectedIndex >= options.Count)
			{
				continue;
			}

			ArcaneEnchantmentOption option = options[selectedIndex];
			Flash();
			CardCmd.Enchant(option.CreateCanonical().ToMutable(), selectedCard, SelectionCount);
			CardCmd.Preview(selectedCard);
		}
	}

	private static bool CanEnchantWithAnyOption(CardModel card)
	{
		return EnchantmentOptions.Any(option => option.CreateCanonical().CanEnchant(card));
	}

	private static IReadOnlyList<ArcaneEnchantmentOption> GetApplicableOptions(CardModel card)
	{
		return EnchantmentOptions
			.Where(option => option.CreateCanonical().CanEnchant(card))
			.ToArray();
	}

	private static bool HasCloneEnchantment(CardModel card)
	{
		return card.Enchantment is Clone
			|| card.Enchantment is SponsorCompositeEnchantment composite && composite.ContainsEnchantmentType(typeof(Clone))
			|| HasExternalRepeatableCloneEnchantment(card);
	}

	private static bool HasExternalRepeatableCloneEnchantment(CardModel card)
	{
		EnchantmentModel? enchantment = card.Enchantment;
		if (enchantment == null || enchantment.GetType().FullName != "RepeatableEnchantments.RepeatableCompositeEnchantment")
		{
			return false;
		}

		MethodInfo? containsMethod = enchantment.GetType().GetMethod(
			"ContainsEnchantmentType",
			BindingFlags.Instance | BindingFlags.Public,
			null,
			[ typeof(Type) ],
			null);
		return containsMethod?.Invoke(enchantment, [ typeof(Clone) ]) is true;
	}

	private static int IndexOfRelic(IReadOnlyList<RelicModel> options, RelicModel selected)
	{
		ModelId selectedId = selected.CanonicalInstance?.Id ?? selected.Id;
		for (int i = 0; i < options.Count; i++)
		{
			ModelId optionId = options[i].CanonicalInstance?.Id ?? options[i].Id;
			if (optionId == selectedId)
			{
				return i;
			}
		}

		return -1;
	}

	private sealed record ArcaneEnchantmentOption(
		Func<RelicModel> CreateChoiceRelic,
		Func<EnchantmentModel> CreateCanonical,
		Func<IEnumerable<IHoverTip>> CreateHoverTips)
	{
		public static ArcaneEnchantmentOption For<TEnchantment>(Func<RelicModel> createChoiceRelic)
			where TEnchantment : EnchantmentModel
		{
			return new ArcaneEnchantmentOption(
				createChoiceRelic,
				() => ModelDb.Enchantment<TEnchantment>(),
				() => HoverTipFactory.FromEnchantment<TEnchantment>());
		}
	}
}

public abstract class ArcaneEnchantmentChoiceRelic<TEnchantment> : RelicModel
	where TEnchantment : EnchantmentModel
{
	private const string ChoiceIconPath = "res://HextechRunes/images/relics/prismaticForge.png";

	public override RelicRarity Rarity => RelicRarity.Event;

	public override string PackedIconPath => ChoiceIconPath;

	protected override string PackedIconOutlinePath => ChoiceIconPath;

	protected override string BigIconPath => ChoiceIconPath;

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
