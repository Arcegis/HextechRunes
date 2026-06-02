using System.Reflection;
using System.Text;
using System.Text.Json;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace HextechRunes;

public sealed class SolidTimeRune : HextechRelicBase
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	private static readonly MethodInfo CardOnPlayMethod = typeof(CardModel).GetMethod("OnPlay", BindingFlags.Instance | BindingFlags.NonPublic)
		?? throw new InvalidOperationException("CardModel.OnPlay was not found.");
	private bool _startedThisCombat;
	private string _removedCardsJson = "";

	[SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
	public string SavedRemovedPowerCardsJson
	{
		get => _removedCardsJson;
		set => _removedCardsJson = value ?? "";
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips
	{
		get
		{
			List<StoredCard> cards = DecodeStoredCards();
			if (cards.Count <= 0)
			{
				yield break;
			}

			yield return new HoverTip(
				new LocString("relics", "solidTimeRune.storedCards.title"),
				BuildStoredCardsDescription(cards))
			{
				ShouldOverrideTextOverflow = true
			};
		}
	}

	public override Task BeforeCombatStart()
	{
		_startedThisCombat = false;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		_startedThisCombat = false;
		return Task.CompletedTask;
	}

#if STS2_104_OR_NEWER
	public override Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
#else
	public override Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
#endif
	{
		return TriggerStoredPowersAtCombatStart(choiceContext, player);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Owner == null
			|| cardPlay.Card.Owner != Owner
			|| cardPlay.Card.Type != CardType.Power
			|| !TryGetDeckPower(cardPlay.Card, out CardModel? deckCard))
		{
			return;
		}

		AppendStoredCard(deckCard!);
		Flash();
		await CardPileCmd.RemoveFromDeck(deckCard!, showPreview: false);
	}

	private async Task TriggerStoredPowersAtCombatStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (_startedThisCombat
			|| player != Owner
			|| Owner == null
			|| Owner.Creature.IsDead
			|| Owner.Creature.CombatState is not HextechCombatState combatState)
		{
			return;
		}

		_startedThisCombat = true;
		List<StoredCard> cards = DecodeStoredCards();
		if (cards.Count == 0)
		{
			return;
		}

		Flash();
		for (int i = 0; i < cards.Count; i++)
		{
			if (CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead)
			{
				break;
			}

			CardModel? card = CreateCombatCard(combatState, cards[i]);
			if (card == null)
			{
				continue;
			}

			card.SetToFreeThisCombat();
			Creature? target = PickTarget(card, combatState, i);
			await ApplyStoredPowerDirectly(choiceContext, card, target);
		}
	}

	private bool TryGetDeckPower(CardModel combatCard, out CardModel? deckCard)
	{
		deckCard = combatCard.DeckVersion;
		return deckCard != null
			&& deckCard.Owner == Owner
			&& deckCard.Pile?.Type == PileType.Deck
			&& deckCard.Type == CardType.Power
			&& Owner.Deck.Cards.Contains(deckCard);
	}

	private Creature? PickTarget(CardModel card, HextechCombatState combatState, int index)
	{
		return card.TargetType switch
		{
			TargetType.AnyEnemy => HextechRuneTargeting.PickRandomHittableEnemy(
				Owner,
				combatState,
				"solid-time",
				combatState.RoundNumber.ToString(),
				index.ToString(),
				card.Id.Entry),
			TargetType.AnyAlly => Owner?.Creature,
			TargetType.AnyPlayer => Owner?.Creature,
			_ => null
		};
	}

	private void AppendStoredCard(CardModel deckCard)
	{
		List<StoredCard> cards = DecodeStoredCards();
		cards.Add(StoredCard.From(deckCard));
		_removedCardsJson = JsonSerializer.Serialize(cards, JsonOptions);
	}

	private static string BuildStoredCardsDescription(IReadOnlyList<StoredCard> cards)
	{
		StringBuilder builder = new();
		for (int i = 0; i < cards.Count; i++)
		{
			CardModel? preview = CreatePreviewCard(cards[i]);
			if (preview == null)
			{
				continue;
			}

			if (builder.Length > 0)
			{
				builder.AppendLine();
			}

			builder.Append("- ");
			builder.Append(preview.Title);
		}

		return builder.Length > 0 ? builder.ToString() : "- ?";
	}

	private static async Task ApplyStoredPowerDirectly(PlayerChoiceContext choiceContext, CardModel card, Creature? target)
	{
		bool addedToTemporaryPlayPile = false;
		if (card.Pile == null)
		{
			await CardPileCmd.Add(card, PileType.Play, skipVisuals: true);
			addedToTemporaryPlayPile = card.Pile?.Type == PileType.Play;
			if (!addedToTemporaryPlayPile)
			{
				Log.Warn($"[{ModInfo.Id}][Mayhem] SolidTime skipped stored power without combat pile: card={card.Id}");
				return;
			}
		}

		CardPlay cardPlay = new()
		{
			Card = card,
			Target = target,
			ResultPile = PileType.None,
			Resources = new ResourceInfo
			{
				EnergySpent = 0,
				EnergyValue = 0,
				StarsSpent = 0,
				StarValue = 0
			},
			IsAutoPlay = true,
			PlayIndex = 0,
			PlayCount = 1
		};

		choiceContext.PushModel(card);
		try
		{
			if (!await TryApplySolidTimeSpecialCase(card))
			{
				await (Task)GetOnPlayMethod(card).Invoke(card, [choiceContext, cardPlay])!;
			}

			if (!card.Owner.Creature.IsDead)
			{
				card.InvokeExecutionFinished();
			}
		}
		finally
		{
			choiceContext.PopModel(card);
			if (addedToTemporaryPlayPile && card.Pile?.IsCombatPile == true)
			{
				await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);
			}
		}
	}

	private static async Task<bool> TryApplySolidTimeSpecialCase(CardModel card)
	{
		if (card is VoidForm)
		{
			await PowerCmd.Apply<VoidFormPower>(
				card.Owner.Creature,
				card.DynamicVars["VoidFormPower"].BaseValue,
				card.Owner.Creature,
				card);
			return true;
		}

		return false;
	}

	private static MethodInfo GetOnPlayMethod(CardModel card)
	{
		Type? type = card.GetType();
		while (type != null)
		{
			MethodInfo? method = type.GetMethod("OnPlay", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
			if (method != null)
			{
				return method;
			}

			type = type.BaseType;
		}

		return CardOnPlayMethod;
	}

	private List<StoredCard> DecodeStoredCards()
	{
		if (string.IsNullOrWhiteSpace(_removedCardsJson))
		{
			return [];
		}

		try
		{
			return (JsonSerializer.Deserialize<List<StoredCard>>(_removedCardsJson, JsonOptions) ?? [])
				.Where(IsStoredPowerCard)
				.ToList();
		}
		catch
		{
			return [];
		}
	}

	private static bool IsStoredPowerCard(StoredCard stored)
	{
		CardModel? canonical = TryGetCanonical(stored);
		return IsStoredAsPowerCard(canonical, stored);
	}

	private static CardModel? CreatePreviewCard(StoredCard stored)
	{
		CardModel? canonical = TryGetCanonical(stored);
		if (canonical == null || !IsStoredAsPowerCard(canonical, stored))
		{
			return null;
		}

		CardModel preview = canonical.ToMutable();
		ApplyStoredCardState(preview, stored);
		ApplyUpgradeLevels(preview, stored.Upgrades);
		return preview;
	}

	private CardModel? CreateCombatCard(HextechCombatState combatState, StoredCard stored)
	{
		if (Owner == null)
		{
			return null;
		}

		CardModel? canonical = TryGetCanonical(stored);
		if (canonical == null || !IsStoredAsPowerCard(canonical, stored))
		{
			return null;
		}

		CardModel card = combatState.CreateCard(canonical, Owner);
		ApplyStoredCardState(card, stored);
		ApplyUpgradeLevels(card, stored.Upgrades);
		SaveManager.Instance.MarkCardAsSeen(card);
		return card;
	}

	private static bool IsStoredAsPowerCard(CardModel? canonical, StoredCard stored)
	{
		if (canonical is MadScience)
		{
			return stored.GetMadScienceCardType() == CardType.Power;
		}

		return canonical?.Type == CardType.Power;
	}

	private static CardModel? TryGetCanonical(StoredCard stored)
	{
		try
		{
			return ModelDb.GetById<CardModel>(new ModelId(stored.Category, stored.Entry));
		}
		catch
		{
			return null;
		}
	}

	private static void ApplyUpgradeLevels(CardModel card, int upgrades)
	{
		int count = Math.Clamp(upgrades, 0, card.MaxUpgradeLevel);
		for (int i = 0; i < count; i++)
		{
			card.UpgradeInternal();
			card.FinalizeUpgradeInternal();
		}
	}

	private static void ApplyStoredCardState(CardModel card, StoredCard stored)
	{
		if (card is MadScience madScience)
		{
			madScience.TinkerTimeType = stored.GetMadScienceCardType();
			madScience.TinkerTimeRider = stored.GetMadScienceRider();
		}
	}

	private sealed record StoredCard(
		string Category,
		string Entry,
		int Upgrades,
		int? MadScienceCardType = null,
		int? MadScienceRider = null)
	{
		public static StoredCard From(CardModel card)
		{
			ModelId id = card.CanonicalInstance.Id;
			if (card is MadScience madScience)
			{
				return new StoredCard(
					id.Category,
					id.Entry,
					card.CurrentUpgradeLevel,
					(int)madScience.TinkerTimeType,
					(int)madScience.TinkerTimeRider);
			}

			return new StoredCard(id.Category, id.Entry, card.CurrentUpgradeLevel);
		}

		public CardType GetMadScienceCardType()
		{
			return MadScienceCardType.HasValue
				? (CardType)MadScienceCardType.Value
				: CardType.Power;
		}

		public TinkerTime.RiderEffect GetMadScienceRider()
		{
			return MadScienceRider.HasValue
				? (TinkerTime.RiderEffect)MadScienceRider.Value
				: TinkerTime.RiderEffect.None;
		}
	}
}
