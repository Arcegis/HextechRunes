using MegaCrit.Sts2.Core.CardSelection;

namespace HextechRunes;

public sealed class ThoughtOverwriteRune : HextechRelicBase
{
	internal const string EtherealMarkerSavedPropertyName = nameof(SavedThoughtOverwriteEtherealMarker);

	public override bool HasUponPickupEffect => true;

	// 只为在 SavedProperty 名称表里登记这个名字，供卡牌存档写入关键词标记(见 HextechThoughtOverwriteKeywordPersistenceHooks)；自身不存值，名称与类型须保留。
	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	private int SavedThoughtOverwriteEtherealMarker
	{
		get => 0;
		set { }
	}

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Replays", 1m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
	];

	public override bool IsAvailableForPlayer(Player player)
	{
		return IsNecrobinderPlayer(player);
	}

	public override async Task AfterObtained()
	{
		// 发放闸门之外，外部接口(RelicBundleGrantHelper)、控制台或其他模组可把本符文直接给任意角色，触发时再判角色。
		if (!IsNecrobinderPlayer(Owner))
		{
			return;
		}

		List<CardModel> selectable = Owner.Deck.Cards.ToList();
		if (selectable.Count == 0)
		{
			return;
		}

		IEnumerable<CardModel> selected = await CardSelectCmd.FromDeckGeneric(
			Owner,
			new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 0, selectable.Count)
			{
				Cancelable = true,
				RequireManualConfirmation = true
			});

		List<CardModel> selectedCards = selected.ToList();
		if (selectedCards.Count == 0)
		{
			return;
		}

		Flash();
		foreach (CardModel card in selectedCards)
		{
			ThoughtOverwriteKeywordPersistence.Track(card);
			CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
		}
	}

	public override Task AfterCardEnteredCombat(CardModel card)
	{
		if (card.Owner != Owner)
		{
			return Task.CompletedTask;
		}

		if (ThoughtOverwriteKeywordPersistence.IsTracked(card.DeckVersion))
		{
			ThoughtOverwriteKeywordPersistence.Restore(card);
		}

		return Task.CompletedTask;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (card.Owner != Owner || !card.Keywords.Contains(CardKeyword.Ethereal))
		{
			return playCount;
		}

		return playCount + DynamicVars["Replays"].IntValue;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		if (card.Owner == Owner && card.Keywords.Contains(CardKeyword.Ethereal))
		{
			Flash();
		}

		return Task.CompletedTask;
	}
}
